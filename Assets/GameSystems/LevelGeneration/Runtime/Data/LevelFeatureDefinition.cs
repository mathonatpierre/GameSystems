using UnityEngine;

namespace GameSystems.LevelGeneration
{
    public abstract class LevelFeatureDefinition : ScriptableObject
    {
        [SerializeField] PlatformSelectionRules selection = new();
        [SerializeField, Min(1f)] float reservedLength = 20f;

        public PlatformSelectionRules Selection => selection;
        public float ReservedLength => reservedLength;

        public void ConfigureReservedLength(float value) => reservedLength = Mathf.Max(1f, value);
    }
}
