using UnityEngine;
using UnityEngine.Playables;

namespace GameSystems.Playables
{
    public abstract class PlayableAnimationAsset : ScriptableObject
    {
        [SerializeField, Min(.001f)] float defaultBlendDuration = .08f;
        [SerializeField] bool restartWhenPlayed = true;

        public float DefaultBlendDuration => defaultBlendDuration;
        public bool RestartWhenPlayed => restartWhenPlayed;

        internal abstract PlayableAnimationRuntime CreateRuntime(PlayableGraph graph);
    }
}
