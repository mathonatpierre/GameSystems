using System;
using System.Collections.Generic;
using UnityEngine;
using GameSystems.Sequencing;

namespace GameSystems.Abilities
{
    [DisallowMultipleComponent]
    public sealed class CharacterContactController : MonoBehaviour, ICharacterContactReceiver
    {
        CharacterAbilityController abilities;
        [SerializeField] CharacterContactRule[] rules;
        readonly List<GameActionRunner> runners = new();

        public event Action<CharacterContactContext> ContactReceived;
        public CharacterContactContext LastContact { get; private set; }
        public IReadOnlyList<CharacterContactRule> Rules => rules ?? Array.Empty<CharacterContactRule>();

        void Awake() => abilities = GetComponent<CharacterAbilityController>();

        void Update()
        {
            for (int i = runners.Count - 1; i >= 0; i--)
                if (runners[i].Tick(Time.deltaTime)) runners.RemoveAt(i);
        }

        public void ReceiveCharacterContact(CharacterAbilityController character, Vector3 point, Vector3 normal)
        {
            Collider collider = character != null ? character.GetComponent<Collider>() : null;
            Publish(new CharacterContactContext(abilities, character, collider, point, normal,
                RelativeVelocity(character), CharacterContactSource.CharacterController));
        }

        public void ReceiveMotorContact(CharacterAbilityController character, Collider collider, Vector3 point,
            Vector3 normal, Vector3 relativeVelocity) =>
            Publish(new CharacterContactContext(abilities, character, collider, point, normal,
                relativeVelocity, CharacterContactSource.Motor));

        void OnCollisionEnter(Collision collision)
        {
            CharacterAbilityController other = collision.collider.GetComponentInParent<CharacterAbilityController>();
            if (collision.contactCount == 0)
            {
                Publish(new CharacterContactContext(abilities, other, collision.collider, transform.position,
                    Vector3.zero, collision.relativeVelocity, CharacterContactSource.Collision));
                return;
            }
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                Publish(new CharacterContactContext(abilities, other, collision.collider, contact.point,
                    contact.normal, collision.relativeVelocity, CharacterContactSource.Collision));
            }
        }

        void OnTriggerEnter(Collider otherCollider)
        {
            CharacterAbilityController other = otherCollider.GetComponentInParent<CharacterAbilityController>();
            Vector3 point = otherCollider.ClosestPoint(transform.position);
            Vector3 normal = (transform.position - point).normalized;
            Publish(new CharacterContactContext(abilities, other, otherCollider, point, normal,
                RelativeVelocity(other), CharacterContactSource.Trigger));
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit == null || hit.collider == null) return;
            CharacterAbilityController other = hit.collider.GetComponentInParent<CharacterAbilityController>();
            ICharacterContactReceiver receiver = hit.collider.GetComponentInParent<ICharacterContactReceiver>();
            receiver?.ReceiveCharacterContact(abilities, hit.point, hit.normal);
            if (other != null && !ReferenceEquals(this, receiver))
                ReceiveCharacterContact(other, hit.point, -hit.normal);
        }

        void Publish(in CharacterContactContext context)
        {
            LastContact = context;
            ContactReceived?.Invoke(context);
            CharacterRuntimeContext character = abilities?.Context;
            if (character == null || rules == null) return;
            GameActionContext actionContext = new(gameObject, context, character, this);
            for (int i = 0; i < rules.Length; i++)
            {
                CharacterContactRule rule = rules[i];
                if (rule == null || !rule.TryStart(actionContext)) continue;
                GameActionRunner runner = rule.Sequence.CreateRunner(actionContext);
                runner.Start();
                if (runner.IsRunning) runners.Add(runner);
                // Rules are ordered from most specific to fallback. One physical
                // contact must never trigger both stomp and side-hit sequences.
                break;
            }
        }

        Vector3 RelativeVelocity(CharacterAbilityController other) =>
            (abilities?.Motor != null ? abilities.Motor.Result.Velocity : Vector3.zero) -
            (other?.Motor != null ? other.Motor.Result.Velocity : Vector3.zero);
    }
}
