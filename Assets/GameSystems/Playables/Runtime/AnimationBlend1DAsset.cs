using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GameSystems.Playables
{
    [CreateAssetMenu(menuName = "Game Systems/Playables/Animation Blend 1D", fileName = "PLAYABLE_Blend1D_")]
    public sealed class AnimationBlend1DAsset : PlayableAnimationAsset
    {
        [SerializeField] string parameter = "Speed";
        [SerializeField] List<AnimationBlend1DSample> samples = new();

        internal override PlayableAnimationRuntime CreateRuntime(PlayableGraph graph) =>
            new BlendRuntime(graph, parameter, samples);

        sealed class BlendRuntime : PlayableAnimationRuntime
        {
            readonly string parameter;
            readonly List<AnimationBlend1DSample> samples;
            readonly AnimationMixerPlayable mixer;
            readonly List<AnimationClipPlayable> clips = new();

            public BlendRuntime(PlayableGraph graph, string parameter, List<AnimationBlend1DSample> samples)
                : base(Create(graph, samples, out AnimationMixerPlayable mixer,
                    out List<AnimationClipPlayable> clips))
            {
                this.parameter = parameter;
                this.samples = samples ?? new List<AnimationBlend1DSample>();
                this.mixer = mixer;
                this.clips = clips;
            }

            static Playable Create(PlayableGraph graph, List<AnimationBlend1DSample> samples,
                out AnimationMixerPlayable mixer, out List<AnimationClipPlayable> clips)
            {
                int count = samples?.Count ?? 0;
                mixer = AnimationMixerPlayable.Create(graph, count);
                clips = new List<AnimationClipPlayable>(count);
                for (int i = 0; i < count; i++)
                {
                    AnimationClipSettings settings = samples[i]?.Animation;
                    AnimationClipPlayable clip = AnimationClipPlayable.Create(graph, settings?.Clip);
                    clip.SetApplyFootIK(false);
                    clip.SetApplyPlayableIK(false);
                    clip.SetSpeed(settings?.Speed ?? 1f);
                    graph.Connect(clip, 0, mixer, i);
                    clips.Add(clip);
                }
                return mixer;
            }

            public override void Evaluate(PlayableAnimationContext context)
            {
                int count = samples.Count;
                if (count == 0) return;
                for (int i = 0; i < count; i++) mixer.SetInputWeight(i, 0f);
                float value = context.GetFloat(parameter);
                if (count == 1 || value <= samples[0].Threshold) { mixer.SetInputWeight(0, 1f); return; }
                if (value >= samples[count - 1].Threshold) { mixer.SetInputWeight(count - 1, 1f); return; }
                for (int i = 0; i < count - 1; i++)
                {
                    float a = samples[i].Threshold;
                    float b = samples[i + 1].Threshold;
                    if (value < a || value > b) continue;
                    float t = Mathf.InverseLerp(a, b, value);
                    mixer.SetInputWeight(i, 1f - t);
                    mixer.SetInputWeight(i + 1, t);
                    return;
                }
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
