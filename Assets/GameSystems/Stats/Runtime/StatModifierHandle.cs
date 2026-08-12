using System;

namespace GameSystems.Stats
{
    public readonly struct StatModifierHandle : IEquatable<StatModifierHandle>
    {
        readonly int value;

        public StatModifierHandle(int value)
        {
            this.value = value;
        }

        public bool IsValid => value != 0;

        public bool Equals(StatModifierHandle other) => value == other.value;
        public override bool Equals(object obj) => obj is StatModifierHandle other && Equals(other);
        public override int GetHashCode() => value;

        public static bool operator ==(StatModifierHandle left, StatModifierHandle right) => left.Equals(right);
        public static bool operator !=(StatModifierHandle left, StatModifierHandle right) => !left.Equals(right);
    }
}
