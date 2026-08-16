using GameSystems.Sequencing;
using System;
using UnityEngine;

namespace GameSystems.Abilities.Conditions
{
    [Serializable]
        public abstract class AbilityCondition : GameCondition
        {
            public bool Evaluate(in AbilityEvaluationContext context)
            {
                bool result = EvaluateAbility(context);
                RecordDebugResult(result);
                return result;
            }
            protected abstract bool EvaluateAbility(in AbilityEvaluationContext context);
            protected sealed override bool OnEvaluate(in GameActionContext context) =>
                context.TryGet(out AbilityEvaluationContext evaluation) && EvaluateAbility(evaluation);
        }

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

    [Serializable]
        public sealed class AbilityLockedCondition : AbilityCondition
        {
            [SerializeField, Tooltip("Expected character ability lock state.")] bool expected = true;
            public override string Summary => expected ? "Abilities are locked" : "Abilities are unlocked";
            protected override bool EvaluateAbility(in AbilityEvaluationContext context)
            {
                IAbilityLockService locks = context.Character.Resolve<IAbilityLockService>();
                return locks != null && locks.IsAbilityLocked == expected;
            }
        }
}
