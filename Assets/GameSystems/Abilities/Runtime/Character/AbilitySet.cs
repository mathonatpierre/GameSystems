using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Abilities
{
    [CreateAssetMenu(menuName = "Game Systems/Abilities/Ability Set", fileName = "ABILITYSET_")]
    public sealed class AbilitySet : ScriptableObject
    {
        [SerializeField] AbilityDefinition[] abilities;
        public IReadOnlyList<AbilityDefinition> Abilities => abilities ?? Array.Empty<AbilityDefinition>();

        public int Count(AbilityCategory category)
        {
            int count = 0;
            IReadOnlyList<AbilityDefinition> items = Abilities;
            for (int i = 0; i < items.Count; i++)
                if (items[i] != null && items[i].Category == category) count++;
            return count;
        }

        public bool Contains(AbilityDefinition definition)
        {
            if (definition == null) return false;
            IReadOnlyList<AbilityDefinition> items = Abilities;
            for (int i = 0; i < items.Count; i++)
                if (items[i] == definition) return true;
            return false;
        }

        public void Configure(IEnumerable<AbilityDefinition> definitions)
        {
            abilities = definitions == null
                ? Array.Empty<AbilityDefinition>()
                : new List<AbilityDefinition>(definitions).ToArray();
        }

    }
}
