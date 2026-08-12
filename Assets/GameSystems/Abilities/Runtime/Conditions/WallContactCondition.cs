using System;
using UnityEngine;

namespace GameSystems.Abilities.Conditions
{
    [Serializable]
    public sealed class WallContactCondition : AbilityCondition
    {
        [SerializeField, Min(0f), Tooltip("Minimum time spent airborne before wall jump is accepted.")] float minimumAirTime = .04f;
        [SerializeField, Range(0f, 1f), Tooltip("Maximum upward component accepted on the wall normal.")] float upwardNormalAllowance = .28f;
        public override string Summary => $"Touching wall after {minimumAirTime:0.###}s airborne";
        protected override bool EvaluateAbility(in AbilityEvaluationContext context)
        {
            CharacterMotorResult motor = context.Motor;
            return context.Character?.Motor != null && !motor.Ground.IsGrounded && motor.AirTime >= minimumAirTime &&
                   motor.Wall.IsTouching && Mathf.Abs(motor.Wall.Normal.y) <= upwardNormalAllowance;
        }
    }
}
