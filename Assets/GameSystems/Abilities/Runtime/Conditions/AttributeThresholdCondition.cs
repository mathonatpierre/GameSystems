using System;
using GameSystems.Stats;
using UnityEngine;

namespace GameSystems.Abilities.Conditions
{
    [Serializable]
    public sealed class AttributeThresholdCondition : AbilityCondition
    {
        [SerializeField, Tooltip("Runtime attribute to inspect.")] AttributeDefinition attribute;
        [SerializeField, Tooltip("Comparison applied to the current attribute value.")] NumericComparison comparison;
        [SerializeField, Tooltip("Value compared against the current attribute value.")] float threshold;

        public AttributeThresholdCondition() { }
        public AttributeThresholdCondition(AttributeDefinition attribute, NumericComparison comparison, float threshold)
        {
            this.attribute = attribute;
            this.comparison = comparison;
            this.threshold = threshold;
        }

        public override string Summary => $"{(attribute != null ? attribute.DisplayName : "Missing attribute")} {comparison} {threshold:0.##}";

        protected override bool EvaluateAbility(in AbilityEvaluationContext context)
        {
            RuntimeAttribute runtime = context.Character.Resolve<CharacterStats>()?.GetAttribute(attribute);
            if (runtime == null) return false;
            return comparison switch
            {
                NumericComparison.Less => runtime.Current < threshold,
                NumericComparison.LessOrEqual => runtime.Current <= threshold,
                NumericComparison.Equal => Mathf.Approximately(runtime.Current, threshold),
                NumericComparison.GreaterOrEqual => runtime.Current >= threshold,
                NumericComparison.Greater => runtime.Current > threshold,
                NumericComparison.NotEqual => !Mathf.Approximately(runtime.Current, threshold),
                _ => false
            };
        }
    }
}
