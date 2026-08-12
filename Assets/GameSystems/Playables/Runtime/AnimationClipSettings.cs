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

        public AnimationClip Clip => clip;
        public float Speed => speed;
        public float NormalizedStart => normalizedStart;
        public float NormalizedEnd => normalizedEnd;
    }
}
