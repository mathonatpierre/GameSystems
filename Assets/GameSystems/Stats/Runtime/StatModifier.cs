using System;

namespace GameSystems.Stats
{
    public enum StatModifierMode { Percentage, Constant }

    [Serializable]
    public readonly struct StatModifier
    {
        public readonly object Source;
        public readonly StatModifierMode Mode;
        public readonly float Value;
        public readonly string Tag;

        public StatModifier(object source, StatModifierMode mode, float value, string tag = null)
        {
            Source = source;
            Mode = mode;
            Value = value;
            Tag = tag;
        }
    }
}
