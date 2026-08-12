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
    }
}
