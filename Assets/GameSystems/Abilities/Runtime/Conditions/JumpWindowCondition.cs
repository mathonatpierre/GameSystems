using System;
using UnityEngine;

using GameSystems.Characters;

namespace GameSystems.Abilities.Conditions
{
    [Serializable]
    public sealed class JumpWindowCondition : AbilityCondition
    {
        [SerializeField, Min(0f), Tooltip("Grace period after leaving ground during which jump remains valid.")] float coyoteTime = .09f;
        [SerializeField, Tooltip("Maximum upward velocity accepted during the coyote window.")] float maximumUpwardVelocity = .12f;
        public override string Summary => $"Grounded or coyote <= {coyoteTime:0.###}s";
        protected override bool EvaluateAbility(in AbilityEvaluationContext context)
        {
            if (context.Character?.Motor == null) return false;
            CharacterMotorResult motor = context.Motor;
            return motor.Ground.IsGrounded || (motor.AirTime <= coyoteTime && motor.Velocity.y <= maximumUpwardVelocity);
        }
    }
}
