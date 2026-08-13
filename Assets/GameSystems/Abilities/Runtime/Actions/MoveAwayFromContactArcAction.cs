using System;
using GameSystems.Sequencing;
using UnityEngine;

using GameSystems.Characters;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class MoveAwayFromContactArcAction : GameAction
    {
        [SerializeField, Min(0f)] float distance = 1.15f;
        [SerializeField, Min(0f)] float height = 1.2f;
        [SerializeField, Min(.01f)] float duration = .58f;
        [SerializeField] bool disableCollider = true;
        public override string Summary => $"Move away from contact {distance:0.##}m, arc {height:0.##}m in {duration:0.##}s";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : GameActionRuntime
        {
            Transform actor;
            ICharacterMotorControl motor;
            Collider collider;
            float elapsed;
            float startX;
            float startY;
            float targetX;
            bool previousColliderEnabled;
            MoveAwayFromContactArcAction Data => (MoveAwayFromContactArcAction)Definition;

            protected override void OnEnter()
            {
                base.OnEnter();
                CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                CharacterAbilityController other = Context.TryGet(out CharacterContactContext contact)
                    ? contact.Other : null;
                if (character == null || other == null) { Fail("No character contact is available."); return; }
                actor = character.Transform;
                motor = character.Motor as ICharacterMotorControl;
                collider = actor.GetComponent<Collider>();
                ICharacterPatrolArea patrol = character.Resolve<ICharacterPatrolArea>();
                float away = actor.position.x >= other.transform.position.x ? 1f : -1f;
                Vector3 world = actor.position;
                startX = world.x;
                startY = world.y;
                targetX = patrol != null
                    ? Mathf.Clamp(startX + away * Data.distance, patrol.MinimumX, patrol.MaximumX)
                    : startX + away * Data.distance;
                if (patrol != null) patrol.Direction = away;
                if (collider != null) { previousColliderEnabled = collider.enabled; if (Data.disableCollider) collider.enabled = false; }
                character.Motor?.ResetMotor();
            }

            protected override bool Tick(float deltaTime)
            {
                if (Failed || actor == null) return true;
                elapsed += deltaTime;
                float t = Mathf.Clamp01(elapsed / Data.duration);
                float eased = t * t * (3f - 2f * t);
                Vector3 world = actor.position;
                world.x = Mathf.Lerp(startX, targetX, eased);
                world.y = startY + 4f * t * (1f - t) * Data.height;
                if (motor != null) motor.Teleport(world); else actor.position = world;
                return t >= 1f;
            }

            protected override void OnExit()
            {
                if (actor != null)
                {
                    Vector3 world = actor.position;
                    world.y = startY;
                    if (motor != null) motor.Teleport(world); else actor.position = world;
                }
                if (collider != null && Data.disableCollider) collider.enabled = previousColliderEnabled;
            }
        }
    }
}
