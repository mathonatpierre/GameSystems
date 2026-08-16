using System;
using UnityEngine;

namespace GameSystems.LevelGeneration
{
    [Serializable]
    public sealed class WallJumpGenerationRules
    {
        [SerializeField, Min(1)] int minimumJumps = 3;
        [SerializeField, Min(1)] int maximumJumps = 7;
        [SerializeField, Min(.5f)] float shaftWidth = 1.35f;
        [SerializeField, Min(.1f)] float verticalSpacing = .8f;
        [SerializeField, Min(2f)] float receptionPlatformWidth = 6f;
        [SerializeField, Range(0f, 1f)] float restLedgeFrequency = .38f;

        public int MinimumJumps => minimumJumps;
        public int MaximumJumps => Mathf.Max(minimumJumps, maximumJumps);
        public float ShaftWidth => shaftWidth;
        public float VerticalSpacing => verticalSpacing;
        public float ReceptionPlatformWidth => Mathf.Max(2f, receptionPlatformWidth);
        public float RestLedgeFrequency => restLedgeFrequency;

        public void Configure(int minimum, int maximum, float width,
            float spacing, float ledgeFrequency, float receptionWidth = 6f)
        {
            minimumJumps = Mathf.Max(1, minimum);
            maximumJumps = Mathf.Max(minimumJumps, maximum);
            shaftWidth = Mathf.Max(.5f, width);
            verticalSpacing = Mathf.Max(.1f, spacing);
            receptionPlatformWidth = Mathf.Max(2f, receptionWidth);
            restLedgeFrequency = Mathf.Clamp01(ledgeFrequency);
        }
    }
}
