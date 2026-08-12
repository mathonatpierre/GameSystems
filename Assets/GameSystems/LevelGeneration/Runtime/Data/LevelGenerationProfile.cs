using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.LevelGeneration
{
    [CreateAssetMenu(fileName = "LevelGenerationProfile", menuName =
        "Game Systems/Level Generation/Profile")]
    public sealed class LevelGenerationProfile : ScriptableObject
    {
        [SerializeField] LevelLayoutRules layout = new();
        [SerializeField] WallJumpGenerationRules wallJumps = new();
        [SerializeField] List<PlatformTypeDefinition> platformTypes = new();

        public int DefaultLength => layout.DefaultLength;
        public int DefaultDepth => layout.DefaultDepth;
        public LevelLayoutRules Layout => layout;
        public WallJumpGenerationRules WallJumps => wallJumps;
        public IReadOnlyList<PlatformTypeDefinition> PlatformTypes => platformTypes;

        public PlatformTypeDefinition Get(PlatformTypeId type)
        {
            for (int i = 0; i < platformTypes.Count; i++)
                if (platformTypes[i] != null && platformTypes[i].Type == type)
                    return platformTypes[i];
            return null;
        }

        public void Configure(int length, int depth,
            IEnumerable<PlatformTypeDefinition> definitions)
        {
            layout.Configure(length, depth, layout.JumpFrequency,
                layout.JumpDifficulty, layout.Verticality, layout.MaximumGap,
                layout.MaximumHeightStep, layout.MaximumAltitude);
            platformTypes.Clear();
            if (definitions != null) platformTypes.AddRange(definitions);
        }

        void OnValidate()
        {
            var used = new HashSet<PlatformTypeId>();
            for (int i = platformTypes.Count - 1; i >= 0; i--)
            {
                PlatformTypeDefinition definition = platformTypes[i];
                if (definition == null || !used.Add(definition.Type))
                    platformTypes.RemoveAt(i);
            }
        }
    }
}
