using System;
using System.Collections.Generic;
using UnityEngine;
using GameSystems.Sequencing;
using GameSystems.Abilities;
using GameSystems.Hooks;
using UnityEngine.Scripting.APIUpdating;

namespace GameSystems.Characters
{
    [MovedFrom(true, "GameSystems.Abilities", "GameSystems.Abilities", "CharacterContactController")]
    [DisallowMultipleComponent]
    public sealed class CharacterContactController : MonoBehaviour, ICharacterContactReceiver,
        ICharacterContactHistory, IContactGateService
    {
        CharacterAbilityController abilities;
        [SerializeField] CharacterContactRule[] rules;
        [SerializeField, Tooltip("Characters with these hooks remain detectable but do not physically collide.")]
        HookId[] passthroughHooks;
        readonly List<GameActionRunner> runners = new();
        readonly List<(Collider own, Collider other)> ignoredCollisionPairs = new();
        readonly HashSet<GameObject> configuredPassthroughTargets = new();
        readonly Dictionary<UnityEngine.Object, CharacterContactContext> pendingContacts = new();
        readonly Dictionary<string, double> gates = new();

        public event Action<CharacterContactContext> ContactReceived;
        public CharacterContactContext LastContact { get; private set; }
        public IReadOnlyList<CharacterContactRule> Rules => rules ?? Array.Empty<CharacterContactRule>();

        public bool IsOpen(string channel) => string.IsNullOrWhiteSpace(channel) ||
            !gates.TryGetValue(channel, out double until) || Time.timeAsDouble >= until;

        public void Close(string channel, float duration)
        {
            if (!string.IsNullOrWhiteSpace(channel))
                gates[channel] = Time.timeAsDouble + Mathf.Max(0f, duration);
        }

        void Awake() => abilities = GetComponent<CharacterAbilityController>();

        void Update()
        {
            RefreshPassthroughCollisions();
            for (int i = runners.Count - 1; i >= 0; i--)
                if (runners[i].Tick(Time.deltaTime)) runners.RemoveAt(i);
        }

        void OnDisable()
        {
            for (int i = 0; i < ignoredCollisionPairs.Count; i++)
            {
                (Collider own, Collider other) = ignoredCollisionPairs[i];
                if (own != null && other != null) Physics.IgnoreCollision(own, other, false);
            }
            ignoredCollisionPairs.Clear();
            configuredPassthroughTargets.Clear();
        }

        void RefreshPassthroughCollisions()
        {
            if (passthroughHooks == null) return;
            Collider[] ownColliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < passthroughHooks.Length; i++)
            {
                GameObject target = HookRegistry.Get(passthroughHooks[i]);
                if (target == null || !configuredPassthroughTargets.Add(target)) continue;
                Collider[] targetColliders = target.GetComponentsInChildren<Collider>(true);
                for (int ownIndex = 0; ownIndex < ownColliders.Length; ownIndex++)
                for (int targetIndex = 0; targetIndex < targetColliders.Length; targetIndex++)
                {
                    Collider own = ownColliders[ownIndex];
                    Collider other = targetColliders[targetIndex];
                    if (own == null || other == null || own == other) continue;
                    Physics.IgnoreCollision(own, other, true);
                    ignoredCollisionPairs.Add((own, other));
                }
            }
        }

        void LateUpdate()
        {
            if (pendingContacts.Count == 0) return;
            foreach (CharacterContactContext context in pendingContacts.Values)
                PublishImmediate(context);
            pendingContacts.Clear();
        }

        public void ReceiveCharacterContact(CharacterAbilityController character, Vector3 point, Vector3 normal)
        {
            Collider collider = character != null ? character.GetComponent<Collider>() : null;
            PublishImmediate(new CharacterContactContext(abilities, character, collider, point, normal,
                RelativeVelocity(character), CharacterContactSource.CharacterController));
        }

        public void ReceiveMotorContact(CharacterAbilityController character, Collider collider, Vector3 point,
            Vector3 normal, Vector3 relativeVelocity) =>
            Queue(new CharacterContactContext(abilities, character, collider, point, normal,
                relativeVelocity, CharacterContactSource.Motor));

        void OnCollisionEnter(Collision collision)
        {
            CharacterAbilityController other = collision.collider.GetComponentInParent<CharacterAbilityController>();
            if (collision.contactCount == 0)
            {
                Queue(new CharacterContactContext(abilities, other, collision.collider, transform.position,
                    Vector3.zero, collision.relativeVelocity, CharacterContactSource.Collision));
                return;
            }
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                Queue(new CharacterContactContext(abilities, other, collision.collider, contact.point,
                    contact.normal, collision.relativeVelocity, CharacterContactSource.Collision));
            }
        }

        void OnTriggerEnter(Collider otherCollider)
        {
            CharacterAbilityController other = otherCollider.GetComponentInParent<CharacterAbilityController>();
            Vector3 point = otherCollider.ClosestPoint(transform.position);
            Vector3 normal = (transform.position - point).normalized;
            Queue(new CharacterContactContext(abilities, other, otherCollider, point, normal,
                RelativeVelocity(other), CharacterContactSource.Trigger));
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit == null || hit.collider == null) return;
            CharacterAbilityController other = hit.collider.GetComponentInParent<CharacterAbilityController>();
            ICharacterContactReceiver receiver = hit.collider.GetComponentInParent<ICharacterContactReceiver>();
            if (receiver is CharacterContactController contactController)
                contactController.ReceiveMotorContact(abilities, GetComponent<Collider>(), hit.point, hit.normal,
                    -RelativeVelocity(other));
            else
                receiver?.ReceiveCharacterContact(abilities, hit.point, hit.normal);
            if (other != null && !ReferenceEquals(this, receiver))
                ReceiveCharacterContact(other, hit.point, -hit.normal);
        }

        void Queue(in CharacterContactContext context)
        {
            UnityEngine.Object key = context.Other != null ? context.Other : context.OtherCollider;
            if (key == null) return;
            if (!pendingContacts.TryGetValue(key, out CharacterContactContext current) ||
                context.Normal.y > current.Normal.y ||
                Mathf.Approximately(context.Normal.y, current.Normal.y) && context.Point.y > current.Point.y)
                pendingContacts[key] = context;
        }

        void PublishImmediate(in CharacterContactContext context)
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
