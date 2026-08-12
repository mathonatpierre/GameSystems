using System;
using UnityEngine;

namespace GameSystems.Abilities.Conditions
{
    [Serializable]
    public sealed class AbilityActiveCondition : AbilityCondition
    {
        [SerializeField, Tooltip("Specific ability to find. Leave empty to match by category.")] AbilityDefinition ability;
        [SerializeField, Tooltip("Category matched when Ability is empty.")] AbilityCategory category = AbilityCategory.Ability;
        [SerializeField, Tooltip("Expected active state.")] bool expected = true;
        public override string Summary => $"{(ability != null ? ability.name : $"Any {category}")} active = {expected.ToString().ToLowerInvariant()}";
        protected override bool EvaluateAbility(in AbilityEvaluationContext context)
        {
            if (context.Character?.Abilities == null) return false;
            var active = context.Character.Abilities.ActiveAbilities;
            bool found = false;
            for (int i = 0; i < active.Count; i++)
            {
                AbilityDefinition candidate = active[i].Definition;
                if (ability != null ? candidate == ability : candidate.Category == category) { found = true; break; }
            }
            return found == expected;
        }
    }
}
