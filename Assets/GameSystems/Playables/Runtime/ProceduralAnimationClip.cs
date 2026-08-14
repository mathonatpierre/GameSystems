using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace GameSystems.Playables
{
    [CreateAssetMenu(menuName = "Game Systems/Playables/Procedural Animation Clip", fileName = "PLAYABLE_Procedural_")]
    public sealed class ProceduralAnimationClip : PlayableAnimationAsset
    {
        [SerializeField] AnimationClipSettings baseAnimation = new();
        [SerializeReference] ProceduralAnimationTrack[] tracks;
        public int TrackCount => tracks?.Length ?? 0;

        internal override PlayableAnimationRuntime CreateRuntime(PlayableGraph graph) =>
            new Runtime(graph, baseAnimation, tracks);

        sealed class Runtime : PlayableAnimationRuntime
        {
            readonly ProceduralAnimationTrack[] tracks;
            readonly AnimationClipSettings baseAnimation;
            readonly AnimationClipPlayable clip;
            public Runtime(PlayableGraph graph, AnimationClipSettings animation,
                ProceduralAnimationTrack[] source) : base(CreateBase(graph, animation, out AnimationClipPlayable clip))
            {
                baseAnimation = animation;
                this.clip = clip;
                if (source == null) { tracks = Array.Empty<ProceduralAnimationTrack>(); return; }
                tracks = new ProceduralAnimationTrack[source.Length];
                for (int i = 0; i < source.Length; i++)
                    tracks[i] = source[i]?.CreateRuntimeCopy();
            }
            public override void Evaluate(PlayableAnimationContext context)
            {
                if (baseAnimation?.Loop == true && baseAnimation.Clip != null)
                {
                    double start = baseAnimation.Clip.length * baseAnimation.NormalizedStart;
                    double end = baseAnimation.Clip.length * baseAnimation.NormalizedEnd;
                    double duration = Mathf.Max(.001f, (float)(end - start));
                    if (clip.GetTime() >= end)
                        clip.SetTime(start + (clip.GetTime() - start) % duration);
                }
                float weight = context.GetFloat("PlayableWeight", 1f);
                for (int i = 0; i < tracks.Length; i++) tracks[i]?.Evaluate(context, Time.deltaTime, weight);
            }
            public override void Restart()
            {
                if (baseAnimation?.Clip != null)
                {
                    clip.SetTime(baseAnimation.Clip.length * baseAnimation.NormalizedStart);
                    clip.SetDone(false);
                }
                for (int i = 0; i < tracks.Length; i++) tracks[i]?.ResetRuntime();
            }

            static Playable CreateBase(PlayableGraph graph, AnimationClipSettings animation,
                out AnimationClipPlayable playable)
            {
                playable = AnimationClipPlayable.Create(graph, animation?.Clip);
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);
                playable.SetSpeed(animation?.Speed ?? 1f);
                return playable;
            }
        }
    }

    [Serializable]
    public abstract class ProceduralAnimationTrack
    {
        [SerializeField] protected string binding = "Body";
        [NonSerialized] protected Transform target;
        protected bool Resolve(PlayableAnimationContext context)
        {
            if (target == null) target = context.ResolveBinding(binding);
            return target != null;
        }
        internal abstract void Evaluate(PlayableAnimationContext context, float deltaTime, float weight);
        internal virtual void ResetRuntime() { target = null; }
        internal ProceduralAnimationTrack CreateRuntimeCopy()
        {
            ProceduralAnimationTrack copy = (ProceduralAnimationTrack)MemberwiseClone();
            copy.ResetRuntime();
            return copy;
        }

        protected T FrameBase<T>(PlayableAnimationContext context, T animatedPose, T proceduralBaseline) =>
            context.HasAnimatorOutput ? animatedPose : proceduralBaseline;
    }

    [Serializable]
    public sealed class RotationFromVelocityTrack : ProceduralAnimationTrack
    {
        [SerializeField] string velocityParameter = "HorizontalSpeed";
        [SerializeField, Min(.01f)] float radius = .35f;
        [NonSerialized] float angle;
        [NonSerialized] Quaternion baseline;
        [NonSerialized] bool initialized;
        internal override void Evaluate(PlayableAnimationContext context, float deltaTime, float weight)
        {
            context.SetFloat("ProceduralTrackEvaluations", context.GetFloat("ProceduralTrackEvaluations") + 1f);
            if (!Resolve(context)) return;
            context.SetFloat("ProceduralResolvedTracks", context.GetFloat("ProceduralResolvedTracks") + 1f);
            if (!initialized) { baseline = target.localRotation; initialized = true; }
            angle = Mathf.Repeat(angle - context.GetFloat(velocityParameter) * deltaTime /
                Mathf.Max(.01f, radius) * Mathf.Rad2Deg, 360f);
            Quaternion frameBase = FrameBase(context, target.localRotation, baseline);
            target.localRotation = Quaternion.Slerp(frameBase, frameBase * Quaternion.Euler(0f, 0f, angle), weight);
            context.SetFloat("ProceduralRotationAngle", angle);
            context.SetFloat("ProceduralAppliedRotation", Quaternion.Angle(frameBase, target.localRotation));
        }
        internal override void ResetRuntime() { base.ResetRuntime(); angle = 0f; initialized = false; }
    }

    [Serializable]
    public sealed class HopCycleTrack : ProceduralAnimationTrack
    {
        [SerializeField, Min(0f)] float height = .29f;
        [SerializeField, Min(0f)] float cyclesPerSecond = 1.48f;
        [NonSerialized] float phase;
        [NonSerialized] Vector3 baseline;
        [NonSerialized] bool initialized;
        internal override void Evaluate(PlayableAnimationContext context, float deltaTime, float weight)
        {
            if (!Resolve(context)) return;
            if (!initialized) { baseline = target.localPosition; initialized = true; }
            phase += deltaTime * cyclesPerSecond * Mathf.PI;
            Vector3 frameBase = FrameBase(context, target.localPosition, baseline);
            Vector3 animated = frameBase + Vector3.up * (Mathf.Abs(Mathf.Sin(phase)) * height);
            target.localPosition = Vector3.Lerp(frameBase, animated, weight);
        }
        internal override void ResetRuntime() { base.ResetRuntime(); phase = 0f; initialized = false; }
    }

    [Serializable]
    public sealed class SquashStretchCycleTrack : ProceduralAnimationTrack
    {
        [SerializeField, Min(0f)] float cyclesPerSecond = 1.48f;
        [SerializeField, Min(0f)] float wobble = .045f;
        [SerializeField, Min(0f)] float landingSquash = .16f;
        [NonSerialized] float phase;
        [NonSerialized] Vector3 baseline;
        [NonSerialized] bool initialized;
        internal override void Evaluate(PlayableAnimationContext context, float deltaTime, float weight)
        {
            if (!Resolve(context)) return;
            if (!initialized) { baseline = target.localScale; initialized = true; }
            phase += deltaTime * cyclesPerSecond * Mathf.PI;
            float hop = Mathf.Abs(Mathf.Sin(phase));
            float wave = Mathf.Sin(phase * 2f) * wobble;
            float squash = Mathf.Pow(1f - hop, 5f) * landingSquash;
            Vector3 frameBase = FrameBase(context, target.localScale, baseline);
            Vector3 animated = Vector3.Scale(frameBase,
                new Vector3(1f + wave + squash, 1f - wave * .72f - squash, 1f + wave + squash));
            target.localScale = Vector3.Lerp(frameBase, animated, weight);
        }
        internal override void ResetRuntime() { base.ResetRuntime(); phase = 0f; initialized = false; }
    }

    [Serializable]
    public sealed class LeanFromVelocityTrack : ProceduralAnimationTrack
    {
        [SerializeField] string velocityParameter = "HorizontalSpeed";
        [SerializeField] float degreesPerUnit = 10f;
        [SerializeField, Min(0f)] float maximumDegrees = 18f;
        [NonSerialized] Quaternion baseline;
        [NonSerialized] bool initialized;
        internal override void Evaluate(PlayableAnimationContext context, float deltaTime, float weight)
        {
            if (!Resolve(context)) return;
            if (!initialized) { baseline = target.localRotation; initialized = true; }
            float angle = Mathf.Clamp(context.GetFloat(velocityParameter) * degreesPerUnit,
                -maximumDegrees, maximumDegrees);
            Quaternion frameBase = FrameBase(context, target.localRotation, baseline);
            target.localRotation = Quaternion.Slerp(frameBase,
                frameBase * Quaternion.Euler(0f, 0f, angle), weight);
        }
        internal override void ResetRuntime() { base.ResetRuntime(); initialized = false; }
    }

    [Serializable]
    public sealed class GlidePoseTrack : ProceduralAnimationTrack
    {
        [SerializeField] Vector3 eulerOffset = new(0f, 0f, -18f);
        [SerializeField, Min(0f)] float breathingDegrees = 2.5f;
        [SerializeField, Min(.01f)] float breathingSpeed = 1.6f;
        [NonSerialized] Quaternion baseline;
        [NonSerialized] bool initialized;

        internal override void Evaluate(PlayableAnimationContext context, float deltaTime, float weight)
        {
            if (!Resolve(context)) return;
            if (!initialized) { baseline = target.localRotation; initialized = true; }
            Quaternion frameBase = FrameBase(context, target.localRotation, baseline);
            float breath = Mathf.Sin(Time.time * breathingSpeed * Mathf.PI * 2f) * breathingDegrees;
            Quaternion pose = frameBase * Quaternion.Euler(eulerOffset + Vector3.forward * breath);
            target.localRotation = Quaternion.Slerp(frameBase, pose, weight);
        }

        internal override void ResetRuntime() { base.ResetRuntime(); initialized = false; }
    }
}
