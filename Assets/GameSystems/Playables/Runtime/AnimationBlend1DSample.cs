using System;
using UnityEngine;

namespace GameSystems.Playables
{
    [Serializable]
    public sealed class AnimationBlend1DSample
    {
        [SerializeField] float threshold;
        [SerializeField] AnimationClipSettings animation = new();

        public float Threshold => threshold;
        public AnimationClipSettings Animation => animation;
    }
}
