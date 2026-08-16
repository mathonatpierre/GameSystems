using System;
using UnityEngine;

namespace GameSystems.Playables
{
    [Serializable]
    public sealed class AnimationClipSettings
    {
        [SerializeField] AnimationClip clip;
        [SerializeField, Min(.01f)] float speed = 1f;
        [SerializeField, Range(0f, 1f)] float normalizedStart;
        [SerializeField, Range(0f, 1f)] float normalizedEnd = 1f;
        [SerializeField] bool loop;

        public AnimationClip Clip => clip;
        public float Speed => speed;
        public float NormalizedStart => normalizedStart;
        public float NormalizedEnd => normalizedEnd;
        public bool Loop => loop;

        public void Configure(AnimationClip value, bool shouldLoop, float playbackSpeed = 1f,
            float start = 0f, float end = 1f)
        {
            clip = value;
            loop = shouldLoop;
            speed = Mathf.Max(.01f, playbackSpeed);
            normalizedStart = Mathf.Clamp01(start);
            normalizedEnd = Mathf.Clamp(end, normalizedStart, 1f);
        }
    }
}
