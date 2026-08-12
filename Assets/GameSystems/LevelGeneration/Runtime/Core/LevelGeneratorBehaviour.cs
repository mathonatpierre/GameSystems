using UnityEngine;
using System;
using System.Collections.Generic;

namespace GameSystems.LevelGeneration
{
    public abstract class LevelGeneratorBehaviour : MonoBehaviour
    {
        [Header("Data-Driven Generation")]
        [SerializeField] LevelGenerationProfile generationProfile;
        [SerializeField] GenerationPass[] passes;

        public LevelGenerationProfile GenerationProfile => generationProfile;
        public IReadOnlyList<GenerationPass> Passes => passes;

        public void ConfigureProfile(LevelGenerationProfile profile)
            => generationProfile = profile;

        public abstract void Generate();

        protected LevelGenerationContext CreateContext(int seed)
            => new(generationProfile, transform, seed);

        protected LevelGenerationReport ExecutePasses(LevelGenerationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (passes == null || passes.Length == 0) return context.Report;

            GenerationPass[] ordered = (GenerationPass[])passes.Clone();
            Array.Sort(ordered, (a, b) => (a?.Order ?? int.MaxValue).CompareTo(b?.Order ?? int.MaxValue));
            for (int i = 0; i < ordered.Length; i++)
            {
                GenerationPass pass = ordered[i];
                if (pass == null || !pass.Enabled) continue;
                pass.Execute(context);
                if (!context.Report.Succeeded) break;
            }

            return context.Report;
        }
    }
}
