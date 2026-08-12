using System.Collections.Generic;

namespace GameSystems.Stats
{
    public sealed class RuntimeStat
    {
        readonly List<Entry> modifiers = new();

        readonly struct Entry
        {
            public readonly StatModifierHandle Handle;
            public readonly StatModifier Modifier;

            public Entry(StatModifierHandle handle, StatModifier modifier)
            {
                Handle = handle;
                Modifier = modifier;
            }
        }

        public StatDefinition Definition { get; }
        public float BaseValue { get; set; }
        public int ModifierCount => modifiers.Count;
        public IEnumerable<StatModifier> Modifiers
        {
            get
            {
                for (int i = 0; i < modifiers.Count; i++)
                    yield return modifiers[i].Modifier;
            }
        }

        public float Value
        {
            get
            {
                float percentage = 0f;
                float constant = 0f;
                for (int i = 0; i < modifiers.Count; i++)
                    if (modifiers[i].Modifier.Mode == StatModifierMode.Percentage) percentage += modifiers[i].Modifier.Value;
                    else constant += modifiers[i].Modifier.Value;
                return BaseValue * (1f + percentage) + constant;
            }
        }

        public RuntimeStat(StatDefinition definition)
        {
            Definition = definition;
            BaseValue = definition != null ? definition.BaseValue : 0f;
        }

        public void AddModifier(StatModifier modifier)
            => AddModifier(default, modifier);

        public void AddModifier(StatModifierHandle handle, StatModifier modifier)
            => modifiers.Add(new Entry(handle, modifier));

        public bool RemoveModifier(StatModifierHandle handle)
        {
            if (!handle.IsValid) return false;
            return modifiers.RemoveAll(item => item.Handle == handle) > 0;
        }

        public void RemoveModifiersFrom(object source)
            => modifiers.RemoveAll(item => ReferenceEquals(item.Modifier.Source, source));

        public void ClearModifiers() => modifiers.Clear();
    }
}
