using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GameSystems.Playables
{
    [CreateAssetMenu(menuName = "Game Systems/Playables/Animation Clip", fileName = "PLAYABLE_Clip_")]
    public sealed class AnimationClipAsset : PlayableAnimationAsset
    {
        [SerializeField] AnimationClipSettings animation = new();

        internal override PlayableAnimationRuntime CreateRuntime(PlayableGraph graph) =>
            new ClipRuntime(graph, animation);

        sealed class ClipRuntime : PlayableAnimationRuntime
        {
            readonly AnimationClipPlayable clip;
            readonly AnimationClipSettings settings;

            public ClipRuntime(PlayableGraph graph, AnimationClipSettings settings)
                : base(Create(graph, settings, out AnimationClipPlayable clip))
            {
                this.clip = clip;
                this.settings = settings;
            }

            static Playable Create(PlayableGraph graph, AnimationClipSettings settings,
                out AnimationClipPlayable playable)
            {
                playable = settings?.Clip != null
                    ? AnimationClipPlayable.Create(graph, settings.Clip)
                    : AnimationClipPlayable.Create(graph, null);
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);
                playable.SetSpeed(settings?.Speed ?? 1f);
                return playable;
            }

            public override void Evaluate(PlayableAnimationContext context) { }

            public override void Restart()
            {
                if (settings?.Clip == null) return;
                clip.SetTime(settings.Clip.length * settings.NormalizedStart);
                clip.SetDone(false);
            }
        }
    }
}
