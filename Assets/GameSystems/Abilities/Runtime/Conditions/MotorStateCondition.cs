using System;
using UnityEngine;

namespace GameSystems.Abilities.Embedded
{
    [Serializable]
    public sealed class MotorStateCondition : AbilityCondition
    {
        [SerializeField, Tooltip("Required character motor state.")] CharacterMotorState state;
        [SerializeField, Tooltip("Vertical velocity tolerance used by rising and descending states.")]
        float verticalDeadZone = .12f;

        public MotorStateCondition() { }
        public MotorStateCondition(CharacterMotorState state, float verticalDeadZone = .12f)
        {
            this.state = state;
            this.verticalDeadZone = verticalDeadZone;
        }

        public override string Summary => $"Motor state is {state}";

        protected override bool EvaluateAbility(in AbilityEvaluationContext context)
        {
            if (context.Character?.Motor == null) return false;
            return state switch
            {
                CharacterMotorState.Grounded => context.Motor.Ground.IsGrounded,
                CharacterMotorState.Airborne => !context.Motor.Ground.IsGrounded,
                CharacterMotorState.Rising => !context.Motor.Ground.IsGrounded && context.Motor.Velocity.y > verticalDeadZone,
                CharacterMotorState.Descending => !context.Motor.Ground.IsGrounded && context.Motor.Velocity.y <= verticalDeadZone,
                _ => false
            };
        }
    }
}
