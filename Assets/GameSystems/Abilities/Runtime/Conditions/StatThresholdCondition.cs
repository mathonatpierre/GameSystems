using System;
using GameSystems.Stats;
using UnityEngine;

namespace GameSystems.Abilities.Conditions
{
    [Serializable]
    public sealed class StatThresholdCondition : AbilityCondition
    {
        [SerializeField, Tooltip("Runtime stat inspected by this condition.")] StatDefinition stat;
        [SerializeField, Tooltip("Comparison applied to the calculated stat value.")] NumericComparison comparison;
        [SerializeField, Tooltip("Value compared against the calculated stat value.")] float threshold;
        public override string Summary => $"{(stat != null ? stat.DisplayName : "Missing stat")} {comparison} {threshold:0.##}";
        protected override bool EvaluateAbility(in AbilityEvaluationContext context)
        {
            CharacterStats stats = context.Character.Resolve<CharacterStats>();
            if (stats == null || stat == null) return false;
            float value = stats.GetStatValue(stat);
            return Compare(value, threshold, comparison);
        }

        internal static bool Compare(float left, float right, NumericComparison comparison) => comparison switch
        {
            NumericComparison.Less => left < right,
            NumericComparison.LessOrEqual => left <= right,
            NumericComparison.Equal => Mathf.Approximately(left, right),
            NumericComparison.GreaterOrEqual => left >= right,
            NumericComparison.Greater => left > right,
            NumericComparison.NotEqual => !Mathf.Approximately(left, right),
            _ => false
        };
    }
}
