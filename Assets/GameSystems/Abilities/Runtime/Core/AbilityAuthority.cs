using System;

namespace GameSystems.Abilities
{
    [Flags]
    public enum AbilityAuthority
    {
        None = 0,
        Horizontal = 1 << 0,
        Vertical = 1 << 1,
        Rotation = 1 << 2,
        Input = 1 << 3,
        Animation = 1 << 4
    }
}
