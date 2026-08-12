using UnityEngine;

namespace GameSystems.Abilities
{
    public readonly struct AbilityRequest
    {
        public readonly AbilityDefinition Ability;
        public readonly Object Source;
        public readonly float Value;
        public readonly double Timestamp;

        public AbilityRequest(AbilityDefinition ability, Object source, float value, double timestamp)
        {
            Ability = ability;
            Source = source;
            Value = value;
            Timestamp = timestamp;
        }
    }
}
