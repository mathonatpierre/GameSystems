using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GameSystems.Playables
{
    [CreateAssetMenu(menuName = "Game Systems/Playables/Animation Blend 2D", fileName = "PLAYABLE_Blend2D_")]
    public sealed class AnimationBlend2DAsset : PlayableAnimationAsset
    {
        [SerializeField] string parameterX = "Horizontal";
        [SerializeField] string parameterY = "Vertical";
        [SerializeField] List<AnimationBlend2DSample> samples = new();

        internal override PlayableAnimationRuntime CreateRuntime(PlayableGraph graph) =>
            new BlendRuntime(graph, parameterX, parameterY, samples);

        sealed class BlendRuntime : PlayableAnimationRuntime
        {
            readonly string parameterX;
            readonly string parameterY;
            readonly List<AnimationBlend2DSample> samples;
            readonly AnimationMixerPlayable mixer;
            readonly List<AnimationClipPlayable> clips;

            public BlendRuntime(PlayableGraph graph, string x, string y, List<AnimationBlend2DSample> samples)
                : base(Create(graph, samples, out AnimationMixerPlayable mixer,
                    out List<AnimationClipPlayable> clips))
            {
                parameterX = x; parameterY = y;
                this.samples = samples ?? new List<AnimationBlend2DSample>();
                this.mixer = mixer; this.clips = clips;
            }

            static Playable Create(PlayableGraph graph, List<AnimationBlend2DSample> samples,
                out AnimationMixerPlayable mixer, out List<AnimationClipPlayable> clips)
            {
                int count = samples?.Count ?? 0;
                mixer = AnimationMixerPlayable.Create(graph, count);
                clips = new List<AnimationClipPlayable>(count);
                for (int i = 0; i < count; i++)
                {
                    AnimationClipSettings settings = samples[i]?.Animation;
                    AnimationClipPlayable clip = AnimationClipPlayable.Create(graph, settings?.Clip);
                    clip.SetApplyFootIK(false); clip.SetApplyPlayableIK(false);
                    clip.SetSpeed(settings?.Speed ?? 1f);
                    graph.Connect(clip, 0, mixer, i); clips.Add(clip);
                }
                return mixer;
            }

            public override void Evaluate(PlayableAnimationContext context)
            {
                if (samples.Count == 0) return;
                Vector2 point = new(context.GetFloat(parameterX), context.GetFloat(parameterY));
                float total = 0f;
                for (int i = 0; i < samples.Count; i++)
                {
                    float weight = 1f / Mathf.Max(.0001f, Vector2.SqrMagnitude(point - samples[i].Position));
                    mixer.SetInputWeight(i, weight); total += weight;
                }
                if (total <= 0f) return;
                for (int i = 0; i < samples.Count; i++) mixer.SetInputWeight(i, mixer.GetInputWeight(i) / total);
            }

            public override void Restart()
            {
                for (int i = 0; i < clips.Count; i++)
                {
                    AnimationClipSettings settings = samples[i]?.Animation;
                    if (settings?.Clip == null) continue;
                    clips[i].SetTime(settings.Clip.length * settings.NormalizedStart);
                    clips[i].SetDone(false);
                }
            }
        }
    }
}
