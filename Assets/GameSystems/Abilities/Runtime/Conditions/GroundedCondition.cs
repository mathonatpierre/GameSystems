using System;
using UnityEngine;

namespace GameSystems.Abilities.Embedded
{
    [Serializable]
    public sealed class GroundedCondition : AbilityCondition
    {
        [SerializeField, Tooltip("Required grounded value. Disable to test for airborne state.")] bool expected = true;

        public GroundedCondition() { }
        public GroundedCondition(bool expected = true) => this.expected = expected;

        public override string Summary => expected ? "Is grounded" : "Is airborne";

        protected override bool EvaluateAbility(in AbilityEvaluationContext context) =>
            context.Character?.Motor != null && context.Motor.Ground.IsGrounded == expected;
    }
}
