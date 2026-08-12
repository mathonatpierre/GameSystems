using System;
using UnityEngine;

namespace GameSystems.Abilities.Embedded
{
    [Serializable]
    public sealed class RequestValueCondition : AbilityCondition
    {
        [SerializeField, Tooltip("Comparison applied to the current ability request payload.")] NumericComparison comparison;
        [SerializeField, Tooltip("Request value threshold.")] float threshold;
        public override string Summary => $"Request value {comparison} {threshold:0.##}";
        protected override bool EvaluateAbility(in AbilityEvaluationContext context) =>
            StatThresholdCondition.Compare(context.Request.Value, threshold, comparison);
    }
}
