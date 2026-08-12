using System;
using UnityEngine;

namespace GameSystems.LevelGeneration
{
    [Serializable]
    public sealed class LevelLayoutRules
    {
        [SerializeField, Min(15)] int defaultLength = 200;
        [SerializeField, Min(1)] int defaultDepth = 3;
        [SerializeField, Range(0f, .8f)] float jumpFrequency = .3f;
        [SerializeField, Range(0f, 1f)] float jumpDifficulty = .4f;
        [SerializeField, Range(0f, 1f)] float verticality = .38f;
        [SerializeField, Min(.5f)] float maximumGap = 4.5f;
        [SerializeField, Min(0f)] float maximumHeightStep = 2.5f;
        [SerializeField, Min(5f)] float maximumAltitude = 40f;

        public int DefaultLength => defaultLength;
        public int DefaultDepth => defaultDepth;
        public float JumpFrequency => jumpFrequency;
        public float JumpDifficulty => jumpDifficulty;
        public float Verticality => verticality;
        public float MaximumGap => maximumGap;
        public float MaximumHeightStep => maximumHeightStep;
        public float MaximumAltitude => maximumAltitude;

        public void Configure(int length, int depth, float frequency, float difficulty,
            float verticalBias, float maxGap, float maxHeightStep, float maxAltitude)
        {
            defaultLength = Mathf.Max(15, length);
            defaultDepth = Mathf.Max(1, depth);
            jumpFrequency = Mathf.Clamp(frequency, 0f, .8f);
            jumpDifficulty = Mathf.Clamp01(difficulty);
            verticality = Mathf.Clamp01(verticalBias);
            maximumGap = Mathf.Max(.5f, maxGap);
            maximumHeightStep = Mathf.Max(0f, maxHeightStep);
            maximumAltitude = Mathf.Max(5f, maxAltitude);
        }
    }
}
