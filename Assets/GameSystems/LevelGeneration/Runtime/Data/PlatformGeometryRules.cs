using System;
using UnityEngine;

namespace GameSystems.LevelGeneration
{
    [Serializable]
    public sealed class PlatformGeometryRules
    {
        [SerializeField, Min(1)] int minimumLength = 3;
        [SerializeField, Min(1)] int maximumLength = 7;
        [SerializeField, Min(1)] int minimumDepth = 3;
        [SerializeField, Min(1)] int maximumDepth = 3;
        [SerializeField, Min(1)] int minimumFoundationDepth = 1;
        [SerializeField, Min(1)] int maximumFoundationDepth = 9;
        [SerializeField, Range(0f, .5f)] float edgeDamage = .04f;
        [SerializeField, Range(0f, .5f)] float surfaceIrregularity = .11f;

        public int MinimumLength => minimumLength;
        public int MaximumLength => Mathf.Max(minimumLength, maximumLength);
        public int MinimumDepth => minimumDepth;
        public int MaximumDepth => Mathf.Max(minimumDepth, maximumDepth);
        public int MinimumFoundationDepth => minimumFoundationDepth;
        public int MaximumFoundationDepth => Mathf.Max(minimumFoundationDepth,
            maximumFoundationDepth);
        public float EdgeDamage => edgeDamage;
        public float SurfaceIrregularity => surfaceIrregularity;

        public void Configure(int minLength, int maxLength, int depth,
            int minFoundation = 1, int maxFoundation = 9)
        {
            minimumLength = Mathf.Max(1, minLength);
            maximumLength = Mathf.Max(minimumLength, maxLength);
            minimumDepth = maximumDepth = Mathf.Max(1, depth);
            minimumFoundationDepth = Mathf.Max(1, minFoundation);
            maximumFoundationDepth = Mathf.Max(minimumFoundationDepth, maxFoundation);
        }
    }
}
