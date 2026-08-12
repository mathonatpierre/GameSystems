using UnityEngine;

namespace GameSystems.Stats
{
    public sealed class RuntimeAttribute
    {
        readonly CharacterStats owner;
        public AttributeDefinition Definition { get; }
        public float Current { get; private set; }
        public float Minimum => Definition.MinimumValue;
        public float Maximum => Definition.MaximumStat != null
            ? owner.GetStatValue(Definition.MaximumStat)
            : Definition.FallbackMaximum;
        public float Normalized => Mathf.InverseLerp(Minimum, Maximum, Current);

        public RuntimeAttribute(AttributeDefinition definition, CharacterStats owner)
        {
            Definition = definition;
            this.owner = owner;
            Current = Mathf.Lerp(Minimum, Maximum, definition.StartPercent);
        }

        internal float Set(float value)
        {
            Current = Mathf.Clamp(value, Minimum, Maximum);
            return Current;
        }
    }
}
