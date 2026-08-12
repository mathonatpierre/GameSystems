using System;
using GameSystems.Playables;
using UnityEngine;
namespace GameSystems.Abilities
{
    [Serializable]
    public sealed class AbilityAnimationIntent
    {
        [SerializeField] PlayableAnimationAsset animation;
        [SerializeField] int priorityOffset;
        [SerializeField] float blendDurationOverride = -1f;

        public PlayableAnimationAsset Animation => animation;
        public int PriorityOffset => priorityOffset;
        public float BlendDuration => blendDurationOverride >= 0f
            ? blendDurationOverride
            : animation != null ? animation.DefaultBlendDuration : .08f;
        public bool IsValid => animation != null;
    }
}
