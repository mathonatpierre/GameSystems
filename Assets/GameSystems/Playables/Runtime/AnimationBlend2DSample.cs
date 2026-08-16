using System;
using UnityEngine;

namespace GameSystems.Playables
{
    [Serializable]
    public sealed class AnimationBlend2DSample
    {
        [SerializeField] Vector2 position;
        [SerializeField] AnimationClipSettings animation = new();

        public Vector2 Position => position;
        public AnimationClipSettings Animation => animation;

        public void Configure(Vector2 blendPosition, AnimationClip clip, bool loop,
            float speed = 1f, float normalizedStart = 0f, float normalizedEnd = 1f)
        {
            position = blendPosition;
            animation.Configure(clip, loop, speed, normalizedStart, normalizedEnd);
        }
    }
}
