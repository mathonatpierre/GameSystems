using System;
using UnityEngine;

namespace GameSystems.Abilities.Embedded
{
    public enum VelocityAxis { Horizontal, Vertical, Magnitude }

    [Serializable]
    public sealed class VelocityCondition : AbilityCondition
    {
        [SerializeField, Tooltip("Velocity component inspected by this condition.")] VelocityAxis axis;
        [SerializeField, Tooltip("Comparison applied to the selected velocity component.")] NumericComparison comparison;
        [SerializeField, Tooltip("Velocity threshold in units per second.")] float threshold;
        public override string Summary => $"{axis} velocity {comparison} {threshold:0.##}";
        protected override bool EvaluateAbility(in AbilityEvaluationContext context)
        {
            Vector3 velocity = context.Motor.Velocity;
            float value = axis switch
            {
                VelocityAxis.Horizontal => Mathf.Abs(velocity.x),
                VelocityAxis.Vertical => velocity.y,
                _ => velocity.magnitude
            };
            return StatThresholdCondition.Compare(value, threshold, comparison);
        }
    }
}
