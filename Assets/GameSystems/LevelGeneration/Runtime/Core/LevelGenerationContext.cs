using GameSystems.Core;
using UnityEngine;

namespace GameSystems.LevelGeneration
{
    public sealed class LevelGenerationContext
    {
        public LevelGenerationContext(LevelGenerationProfile profile, Transform root, int seed)
        {
            Profile = profile;
            Root = root;
            Seed = seed;
            Random = new System.Random(seed);
        }

        public LevelGenerationProfile Profile { get; }
        public Transform Root { get; }
        public int Seed { get; }
        public System.Random Random { get; }
        public GameServiceRegistry Services { get; } = new();
        public LevelGenerationReport Report { get; } = new();
    }
}
