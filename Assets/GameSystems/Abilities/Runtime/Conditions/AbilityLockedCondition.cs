using System;
using UnityEngine;

namespace GameSystems.Abilities.Conditions
{
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
