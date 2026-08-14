using UnityEngine;
using UnityEngine.Playables;

namespace GameSystems.Playables
{
    public abstract class PlayableAnimationAsset : ScriptableObject
    {
        [SerializeField, Min(.001f)] float defaultBlendDuration = .08f;
        [SerializeField] bool restartWhenPlayed = true;
        [SerializeField, Range(-180f, 180f), Tooltip("Yaw applied by character presentation after evaluating this playable.")]
        float facingOffset;

        public float DefaultBlendDuration => defaultBlendDuration;
        public bool RestartWhenPlayed => restartWhenPlayed;
        public float FacingOffset => facingOffset;

        internal abstract PlayableAnimationRuntime CreateRuntime(PlayableGraph graph);
    }
}
