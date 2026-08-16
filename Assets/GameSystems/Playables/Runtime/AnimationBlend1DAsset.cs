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
        [SerializeField] bool synchronizeCycles;
        [SerializeField, Min(.01f)] float synchronizedCycleDuration = 1f;
        [SerializeField] List<AnimationBlend1DSample> samples = new();

        internal override PlayableAnimationRuntime CreateRuntime(PlayableGraph graph) =>
            new BlendRuntime(graph, parameter, synchronizeCycles,
                synchronizedCycleDuration, samples);

        public void Configure(string blendParameter,
            IEnumerable<AnimationBlend1DSample> values, float cycleDuration = 1f)
        {
            parameter = blendParameter;
            synchronizeCycles = true;
            synchronizedCycleDuration = Mathf.Max(.01f, cycleDuration);
            samples.Clear();
            if (values != null) samples.AddRange(values);
        }

        sealed class BlendRuntime : PlayableAnimationRuntime
        {
            readonly string parameter;
            readonly List<AnimationBlend1DSample> samples;
            readonly AnimationMixerPlayable mixer;
            readonly List<AnimationClipPlayable> clips = new();

            readonly bool synchronizeCycles;

            public BlendRuntime(PlayableGraph graph, string parameter, bool synchronize,
                float cycleDuration,
                List<AnimationBlend1DSample> samples)
                : base(Create(graph, samples, out AnimationMixerPlayable mixer,
                    out List<AnimationClipPlayable> clips, synchronize, cycleDuration))
            {
                this.parameter = parameter;
                this.samples = samples ?? new List<AnimationBlend1DSample>();
                this.mixer = mixer;
                this.clips = clips;
                synchronizeCycles = synchronize;
            }

            static Playable Create(PlayableGraph graph, List<AnimationBlend1DSample> samples,
                out AnimationMixerPlayable mixer, out List<AnimationClipPlayable> clips,
                bool synchronize, float cycleDuration)
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
                    float speed = settings?.Speed ?? 1f;
                    if (synchronize && settings?.Clip != null)
                    {
                        float trimmedDuration = settings.Clip.length * Mathf.Max(.001f,
                            settings.NormalizedEnd - settings.NormalizedStart);
                        speed *= trimmedDuration / Mathf.Max(.01f, cycleDuration);
                    }
                    clip.SetSpeed(speed);
                    graph.Connect(clip, 0, mixer, i);
                    clips.Add(clip);
                }
                return mixer;
            }

            public override void Evaluate(PlayableAnimationContext context)
            {
                int count = samples.Count;
                if (count == 0) return;
                if (synchronizeCycles)
                {
                    float phase = GetPhase(0);
                    for (int i = 0; i < count; i++) SetPhase(i, phase);
                }
                else
                {
                    for (int i = 0; i < count; i++) WrapLoop(i);
                }
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

            public override float NormalizedTime => samples.Count > 0 ? GetPhase(0) : 0f;

            public override void SeekNormalized(float normalizedTime)
            {
                float phase = Mathf.Repeat(normalizedTime, 1f);
                for (int i = 0; i < clips.Count; i++) SetPhase(i, phase);
            }

            float GetPhase(int index)
            {
                if (index < 0 || index >= clips.Count) return 0f;
                AnimationClipSettings settings = samples[index]?.Animation;
                if (settings?.Clip == null) return 0f;
                double start = settings.Clip.length * settings.NormalizedStart;
                double duration = Mathf.Max(.001f,
                    settings.Clip.length * (settings.NormalizedEnd - settings.NormalizedStart));
                return Mathf.Repeat((float)((clips[index].GetTime() - start) / duration), 1f);
            }

            void SetPhase(int index, float phase)
            {
                if (index < 0 || index >= clips.Count) return;
                AnimationClipSettings settings = samples[index]?.Animation;
                if (settings?.Clip == null) return;
                double start = settings.Clip.length * settings.NormalizedStart;
                double duration = Mathf.Max(.001f,
                    settings.Clip.length * (settings.NormalizedEnd - settings.NormalizedStart));
                clips[index].SetTime(start + duration * Mathf.Repeat(phase, 1f));
                clips[index].SetDone(false);
            }

            void WrapLoop(int index)
            {
                AnimationClipSettings settings = samples[index]?.Animation;
                if (settings?.Loop != true || settings.Clip == null) return;
                double start = settings.Clip.length * settings.NormalizedStart;
                double end = settings.Clip.length * settings.NormalizedEnd;
                double duration = Mathf.Max(.001f, (float)(end - start));
                if (clips[index].GetTime() >= end)
                    clips[index].SetTime(start + (clips[index].GetTime() - start) % duration);
            }
        }
    }
}
