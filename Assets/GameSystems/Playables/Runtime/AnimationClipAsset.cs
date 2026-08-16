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

            public override void Evaluate(PlayableAnimationContext context)
            {
                if (!settings.Loop || settings.Clip == null) return;
                double start = settings.Clip.length * settings.NormalizedStart;
                double end = settings.Clip.length * settings.NormalizedEnd;
                double duration = Mathf.Max(.001f, (float)(end - start));
                if (clip.GetTime() >= end) clip.SetTime(start + (clip.GetTime() - start) % duration);
            }

            public override float NormalizedTime
            {
                get
                {
                    if (settings?.Clip == null) return 0f;
                    double start = settings.Clip.length * settings.NormalizedStart;
                    double duration = Mathf.Max(.001f,
                        settings.Clip.length * (settings.NormalizedEnd - settings.NormalizedStart));
                    return (float)((clip.GetTime() - start) / duration);
                }
            }

            public override void SeekNormalized(float normalizedTime)
            {
                if (settings?.Clip == null) return;
                double start = settings.Clip.length * settings.NormalizedStart;
                double duration = Mathf.Max(.001f,
                    settings.Clip.length * (settings.NormalizedEnd - settings.NormalizedStart));
                clip.SetTime(start + duration * normalizedTime);
                clip.SetDone(false);
            }

            public override void Restart()
            {
                if (settings?.Clip == null) return;
                clip.SetTime(settings.Clip.length * settings.NormalizedStart);
                clip.SetDone(false);
            }
        }
    }
}
