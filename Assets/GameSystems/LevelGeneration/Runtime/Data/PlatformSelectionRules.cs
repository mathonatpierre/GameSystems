using System;
using UnityEngine;

namespace GameSystems.LevelGeneration
{
    [Serializable]
    public sealed class PlatformSelectionRules
    {
        [SerializeField] bool enabled = true;
        [SerializeField, Min(0f)] float weight = 1f;
        [SerializeField, Range(0f, 1f)] float frequency = .1f;
        [SerializeField, Min(0)] int minimumOccurrences;
        [SerializeField, Min(0)] int maximumOccurrences = 99;
        [SerializeField, Min(0f)] float minimumLevelProgress;
        [SerializeField, Range(0f, 1f)] float maximumLevelProgress = 1f;
        [SerializeField, Min(0f)] float minimumDistanceFromPrevious = 4f;
        [SerializeField, Min(1)] int minimumClusterSize = 1;
        [SerializeField, Min(1)] int maximumClusterSize = 1;

        public bool Enabled => enabled;
        public float Weight => weight;
        public float Frequency => frequency;
        public int MinimumOccurrences => minimumOccurrences;
        public int MaximumOccurrences => Mathf.Max(minimumOccurrences, maximumOccurrences);
        public float MinimumLevelProgress => Mathf.Clamp01(minimumLevelProgress);
        public float MaximumLevelProgress => Mathf.Max(MinimumLevelProgress,
            Mathf.Clamp01(maximumLevelProgress));
        public float MinimumDistanceFromPrevious => minimumDistanceFromPrevious;
        public int MinimumClusterSize => minimumClusterSize;
        public int MaximumClusterSize => Mathf.Max(minimumClusterSize, maximumClusterSize);

        public void Configure(float newFrequency, int minimum = 0, int maximum = 99,
            float minimumProgress = 0f, float maximumProgress = 1f)
        {
            frequency = Mathf.Clamp01(newFrequency);
            minimumOccurrences = Mathf.Max(0, minimum);
            maximumOccurrences = Mathf.Max(minimumOccurrences, maximum);
            minimumLevelProgress = Mathf.Clamp01(minimumProgress);
            maximumLevelProgress = Mathf.Clamp(maximumProgress, minimumLevelProgress, 1f);
        }

        public void ConfigureCluster(int minimum, int maximum)
        {
            minimumClusterSize = Mathf.Max(1, minimum);
            maximumClusterSize = Mathf.Max(minimumClusterSize, maximum);
        }
    }
}
