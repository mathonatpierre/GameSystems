using System;
using UnityEngine;

namespace GameSystems.Abilities.Embedded
{
    [Serializable]
    public sealed class AbilityCooldownCondition : AbilityCondition
    {
        [SerializeField, Tooltip("Ability whose cooldown is inspected.")] AbilityDefinition ability;
        [SerializeField, Tooltip("Expected cooldown-ready state.")] bool ready = true;
        public override string Summary => $"{(ability != null ? ability.name : "Missing ability")} cooldown ready = {ready.ToString().ToLowerInvariant()}";
        protected override bool EvaluateAbility(in AbilityEvaluationContext context)
        {
            if (ability == null || context.Character?.Abilities == null) return false;
            bool isReady = context.Character.Abilities.GetCooldownRemaining(ability) <= 0f;
            return isReady == ready;
        }
    }
}
