using System;
using UnityEngine;

namespace GameSystems.Abilities.Conditions
{
    [Serializable]
    public sealed class AirTimeCondition : AbilityCondition
    {
        [SerializeField, Tooltip("Comparison applied to the current motor air time.")] NumericComparison comparison = NumericComparison.GreaterOrEqual;
        [SerializeField, Min(0f), Tooltip("Air-time threshold in seconds.")] float seconds = .1f;
        public override string Summary => $"Air time {comparison} {seconds:0.###}s";
        protected override bool EvaluateAbility(in AbilityEvaluationContext context) =>
            context.Character?.Motor != null && StatThresholdCondition.Compare(context.Motor.AirTime, seconds, comparison);
    }
}
