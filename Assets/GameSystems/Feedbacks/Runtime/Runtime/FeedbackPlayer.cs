using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameSystems.Feedbacks
{
    [DisallowMultipleComponent]
    public sealed class FeedbackPlayer : MonoBehaviour
    {
        public static bool GlobalFeedbacksEnabled = true;
        [SerializeField] bool canPlay = true;
        [SerializeField] bool autoPlayOnEnable;
        [SerializeField] FeedbackDirection direction;
        [SerializeField, Min(.01f)] float durationMultiplier = 1f;
        [SerializeField, Min(0f)] float constantIntensity = 1f;
        [SerializeField, Range(0f, 100f)] float chanceToPlay = 100f;
        [SerializeField, Min(0f)] float cooldown;
        [Tooltip("Optional sequence used by a persistent preview/autoplay Player.")]
        [SerializeField] FeedbackSequence sequence;
        [SerializeField] List<FeedbackBinding> bindings = new();
        readonly List<Coroutine> running = new();
        readonly Dictionary<Transform, (Vector3 position, Quaternion rotation, Vector3 scale)> initialTransforms = new();
        readonly Dictionary<FeedbackCue, float> cueLastPlayedAt = new();
        float lastPlayedAt = float.NegativeInfinity;
        bool paused;
        int activeCueCount;
        FeedbackContext runtimeContext;

        public FeedbackSequence Sequence { get => sequence; set { sequence = value; CacheInitialValues(); } }
        public bool IsPlaying => activeCueCount > 0;

        void OnEnable() { CacheInitialValues(); if (autoPlayOnEnable && Application.isPlaying) PlayFeedbacks(); }
        void OnDisable() => StopFeedbacks(true);

        public void SetSequence(FeedbackSequence value) { sequence = value; CacheInitialValues(); }

        public void Bind(string id, UnityEngine.Object target)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            FeedbackBinding binding = bindings.Find(item => item != null && item.id == id);
            if (binding == null) { binding = new FeedbackBinding { id = id }; bindings.Add(binding); }
            binding.target = target;
            CacheInitialValues();
        }

        public void PlaySequence(FeedbackSequence sequence, FeedbackContext context)
        {
            runtimeContext = context;
            SetSequence(sequence);
            if (context != null)
            {
                transform.SetPositionAndRotation(context.Position, context.Rotation);
                PlayFeedbacks(context.Position, context.Intensity);
            }
            else PlayFeedbacks();
        }

        public void ClearRuntimeContext() { runtimeContext = null; sequence = null; }

        public void PlayFeedbacks() => PlayFeedbacks(transform.position, 1f);
        public void PlayFeedbacks(Vector3 position, float intensity = 1f)
        {
            if (!GlobalFeedbacksEnabled || !canPlay || Time.unscaledTime < lastPlayedAt + cooldown || UnityEngine.Random.value * 100f > chanceToPlay) return;
            lastPlayedAt = Time.unscaledTime;
            StopFeedbacks(false);
            List<FeedbackCue> source = BuildRuntimeSequence();
            IEnumerable<FeedbackCue> playback = direction == FeedbackDirection.Forward ? source : Reverse(source);
            if (sequence != null && sequence.PlayMode == FeedbackPlayMode.Sequential)
            {
                activeCueCount = 1;
                running.Add(StartCoroutine(PlaySequential(playback, position, intensity)));
                return;
            }
            foreach (FeedbackCue cue in playback)
                if (CanPlay(cue, intensity)) { activeCueCount++; running.Add(StartCoroutine(PlayCue(cue, position, intensity))); }
        }

        public void PlayFeedbacksInReverse(Vector3 position, float intensity = 1f)
        { direction = FeedbackDirection.Reverse; PlayFeedbacks(position, intensity); }
        public void ChangeDirection() => direction = direction == FeedbackDirection.Forward ? FeedbackDirection.Reverse : FeedbackDirection.Forward;
        public void SetCanPlay(bool value) => canPlay = value;
        public void PauseFeedbacks() => paused = true;
        public void ResumeFeedbacks() => paused = false;

        public void StopFeedbacks(bool restoreInitialValues = false)
        {
            foreach (Coroutine coroutine in running) if (coroutine != null) StopCoroutine(coroutine);
            FeedbackTime.ReleaseAll(this);
            running.Clear(); activeCueCount = 0; paused = false;
            if (restoreInitialValues) RestoreInitialValues();
        }

        public void ResetFeedbacks()
        {
            StopFeedbacks(true); lastPlayedAt = float.NegativeInfinity;
            cueLastPlayedAt.Clear();
        }

        public void RestoreInitialValues()
        {
            foreach (var pair in initialTransforms)
                if (pair.Key != null) { pair.Key.localPosition = pair.Value.position; pair.Key.localRotation = pair.Value.rotation; pair.Key.localScale = pair.Value.scale; }
        }

        void CacheInitialValues()
        {
            initialTransforms.Clear();
            foreach (FeedbackCue cue in BuildRuntimeSequence())
            {
                if (cue == null) continue;
                Transform target = ResolveTransformTarget(cue);
                if (target == null) continue;
                if (!initialTransforms.ContainsKey(target)) initialTransforms[target] = (target.localPosition, target.localRotation, target.localScale);
            }
        }

        bool CanPlay(FeedbackCue cue, float intensity)
        {
            if (cue == null || !cue.enabled || intensity < cue.intensityRange.x || intensity > cue.intensityRange.y) return false;
            if (direction == FeedbackDirection.Forward && !cue.forward || direction == FeedbackDirection.Reverse && !cue.reverse) return false;
            cueLastPlayedAt.TryGetValue(cue, out float lastCuePlay);
            return (!cueLastPlayedAt.ContainsKey(cue) || Time.unscaledTime >= lastCuePlay + cue.cooldown) && UnityEngine.Random.value * 100f <= cue.chance;
        }

        IEnumerator PlayCue(FeedbackCue cue, Vector3 position, float intensity)
        {
            cueLastPlayedAt[cue] = Time.unscaledTime;
            float appliedIntensity = cue.constantIntensity ? constantIntensity : intensity * constantIntensity;
            yield return Wait(cue.initialDelay * durationMultiplier, cue.timeMode);
            for (int repeat = 0; repeat <= cue.repeats; repeat++)
            {
                yield return Execute(cue, position, appliedIntensity);
                if (repeat < cue.repeats) yield return Wait(cue.repeatDelay * durationMultiplier, cue.timeMode);
            }
            activeCueCount = Mathf.Max(0, activeCueCount - 1);
        }

        IEnumerator PlaySequential(IEnumerable<FeedbackCue> cues, Vector3 position, float intensity)
        {
            foreach (FeedbackCue cue in cues)
            {
                if (!CanPlay(cue, intensity)) continue;
                cueLastPlayedAt[cue] = Time.unscaledTime;
                float appliedIntensity = cue.constantIntensity ? constantIntensity : intensity * constantIntensity;
                yield return Wait(cue.initialDelay * durationMultiplier, cue.timeMode);
                for (int repeat = 0; repeat <= cue.repeats; repeat++)
                {
                    yield return Execute(cue, position, appliedIntensity);
                    if (repeat < cue.repeats) yield return Wait(cue.repeatDelay * durationMultiplier, cue.timeMode);
                }
            }
            activeCueCount = 0;
        }

        IEnumerator Execute(FeedbackCue cue, Vector3 position, float intensity)
        {
            Transform target = ResolveTransformTarget(cue);
            if (target == null) yield break;
            ParticleSystem particles = Resolve<ParticleSystem>(cue.bindingId) ?? cue.particles;
            AudioSource audioSource = Resolve<AudioSource>(cue.bindingId) ?? cue.audioSource;
            Light targetLight = Resolve<Light>(cue.bindingId) ?? cue.light;
            Renderer targetRenderer = Resolve<Renderer>(cue.bindingId) ?? cue.renderer;
            float duration = Mathf.Max(.001f, cue.duration * durationMultiplier);
            if (cue.kind == FeedbackKind.ParticleBurst) { if (particles != null) particles.Play(); yield break; }
            if (cue.kind == FeedbackKind.Audio) { if (audioSource != null && cue.audioClip != null) audioSource.PlayOneShot(cue.audioClip, intensity * cue.amount); yield break; }
            if (cue.kind == FeedbackKind.AudioRandomized)
            {
                if (audioSource != null && cue.audioClips != null && cue.audioClips.Length > 0)
                {
                    AudioClip clip = cue.audioClips[UnityEngine.Random.Range(0, cue.audioClips.Length)];
                    audioSource.pitch = UnityEngine.Random.Range(cue.pitchRange.x, cue.pitchRange.y);
                    audioSource.PlayOneShot(clip, UnityEngine.Random.Range(cue.volumeRange.x, cue.volumeRange.y) * intensity);
                }
                yield break;
            }
            if (cue.kind == FeedbackKind.InstantiatePooled) { FeedbackPool.Spawn(cue.prefab, position, Quaternion.identity, duration); yield break; }
            if (cue.kind == FeedbackKind.SetActive) { GameObject go = Resolve<GameObject>(cue.bindingId); if (go != null) go.SetActive(cue.amount > 0f); yield break; }
            if (cue.kind == FeedbackKind.AnimatorTrigger) { Animator animator = Resolve<Animator>(cue.bindingId); if (animator != null && !string.IsNullOrEmpty(cue.animatorParameter)) animator.SetTrigger(cue.animatorParameter); yield break; }
            if (cue.kind == FeedbackKind.RigidbodyImpulse) { Rigidbody body = Resolve<Rigidbody>(cue.bindingId); if (body != null && !body.isKinematic) body.AddForce(cue.vector * intensity, cue.forceMode); yield break; }
            if (cue.kind == FeedbackKind.UnityEvent) { cue.unityEvent?.Invoke(); yield break; }
            if (cue.kind == FeedbackKind.NestedPlayer) { FeedbackPlayer nested = Resolve<FeedbackPlayer>(cue.bindingId); if (nested != null && nested != this) nested.PlayFeedbacks(position, intensity); yield break; }
            if (cue.kind == FeedbackKind.FreezeFrame) { yield return FeedbackTime.Freeze(this, cue.amount <= 0f ? .03f : cue.amount, duration); yield break; }
            if (cue.kind == FeedbackKind.RendererBlink)
            {
                GameObject hierarchy = Resolve<GameObject>(cue.bindingId) ?? target.gameObject;
                Renderer[] renderers = hierarchy.GetComponentsInChildren<Renderer>(true);
                bool[] original = new bool[renderers.Length];
                for (int i = 0; i < renderers.Length; i++) original[i] = renderers[i].enabled;
                float elapsedBlink = 0f;
                while (elapsedBlink < duration)
                {
                    while (paused) yield return null;
                    elapsedBlink += cue.timeMode == FeedbackTimeMode.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                    bool visible = Mathf.FloorToInt(elapsedBlink * Mathf.Max(1f, cue.frequency)) % 2 == 0;
                    for (int i = 0; i < renderers.Length; i++)
                        if (renderers[i] != null) renderers[i].enabled = visible && original[i];
                    yield return null;
                }
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null) renderers[i].enabled = original[i];
                yield break;
            }
            if (cue.kind == FeedbackKind.CameraShake)
            {
                MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is not ICameraShakeReceiver receiver) continue;
                    receiver.AddImpactShake(cue.amount * intensity, duration);
                    break;
                }
                yield break;
            }
            Vector3 startPosition = target.localPosition, startScale = target.localScale;
            Quaternion startRotation = target.localRotation;
            float startLight = targetLight != null ? targetLight.intensity : 0f;
            MaterialPropertyBlock block = targetRenderer != null || cue.kind == FeedbackKind.MaterialFloatHierarchy ? new MaterialPropertyBlock() : null;
            int property = Shader.PropertyToID(cue.propertyName);
            float elapsed = 0f;
            float previousTimeScale = Time.timeScale;
            Camera targetCamera = Resolve<Camera>(cue.bindingId) ?? cue.camera ?? Camera.main;
            CanvasGroup targetCanvas = Resolve<CanvasGroup>(cue.bindingId) ?? cue.canvasGroup;
            Volume targetVolume = Resolve<Volume>(cue.bindingId) ?? cue.volume;
            float initialFov = targetCamera != null ? targetCamera.fieldOfView : 0f;
            float initialAlpha = targetCanvas != null ? targetCanvas.alpha : 0f;
            Bloom bloom = null; ChromaticAberration chromatic = null; LensDistortion lens = null; Vignette vignette = null; ColorAdjustments grading = null;
            if (targetVolume != null && targetVolume.profile != null)
            {
                targetVolume.profile.TryGet(out bloom); targetVolume.profile.TryGet(out chromatic);
                targetVolume.profile.TryGet(out lens); targetVolume.profile.TryGet(out vignette); targetVolume.profile.TryGet(out grading);
            }
            float initialVolumeValue = GetVolumeValue(cue.kind, bloom, chromatic, lens, vignette, grading);
            Vector3 springVelocity = Vector3.zero;
            Vector3 springPosition = startPosition;
            Vector3 springScale = startScale;
            Vector3 springEuler = startRotation.eulerAngles;
            Renderer[] hierarchyRenderers = cue.kind == FeedbackKind.MaterialFloatHierarchy
                ? (Resolve<GameObject>(cue.bindingId) ?? target.gameObject).GetComponentsInChildren<Renderer>(true)
                : null;
            while (elapsed < duration)
            {
                while (paused) yield return null;
                if (RequiresTransform(cue.kind) && target == null) yield break;
                elapsed += cue.timeMode == FeedbackTimeMode.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                if (direction == FeedbackDirection.Reverse) normalized = 1f - normalized;
                float value = cue.curve == null ? normalized : cue.curve.Evaluate(normalized);
                bool targetWasDestroyed = false;
                try
                {
                    switch (cue.kind)
                    {
                        case FeedbackKind.TransformShake:
                            float phase = elapsed * cue.frequency;
                            target.localPosition = startPosition + Vector3.Scale(cue.vector, new Vector3(Mathf.Sin(phase), Mathf.Sin(phase * .73f + .8f), Mathf.Sin(phase * .91f + 1.4f))) * value * intensity;
                            break;
                        case FeedbackKind.Scale: target.localScale = Vector3.LerpUnclamped(startScale, Vector3.Scale(startScale, cue.vectorB), value * intensity); break;
                        case FeedbackKind.Position: target.localPosition = Vector3.LerpUnclamped(startPosition, startPosition + cue.vector, value * intensity); break;
                        case FeedbackKind.Rotation: target.localRotation = Quaternion.SlerpUnclamped(startRotation, startRotation * Quaternion.Euler(cue.vector), value * intensity); break;
                        case FeedbackKind.LightIntensity: if (targetLight != null) targetLight.intensity = Mathf.LerpUnclamped(startLight, cue.amount, value * intensity); break;
                        case FeedbackKind.MaterialFloat:
                            if (targetRenderer != null) { targetRenderer.GetPropertyBlock(block); block.SetFloat(property, cue.amount * value * intensity); targetRenderer.SetPropertyBlock(block); }
                            break;
                        case FeedbackKind.MaterialColor:
                            if (targetRenderer != null) { targetRenderer.GetPropertyBlock(block); block.SetColor(property, Color.Lerp(Color.clear, cue.color, value * intensity)); targetRenderer.SetPropertyBlock(block); }
                            break;
                        case FeedbackKind.MaterialFloatHierarchy:
                            if (hierarchyRenderers != null)
                                foreach (Renderer hierarchyRenderer in hierarchyRenderers)
                                {
                                    if (hierarchyRenderer == null) continue;
                                    hierarchyRenderer.GetPropertyBlock(block);
                                    block.SetFloat(property, cue.amount * value * intensity);
                                    hierarchyRenderer.SetPropertyBlock(block);
                                }
                            break;
                        case FeedbackKind.TimeScale: Time.timeScale = Mathf.Lerp(previousTimeScale, cue.amount, value * intensity); break;
                        case FeedbackKind.CameraZoom: if (targetCamera != null) targetCamera.fieldOfView = Mathf.LerpUnclamped(initialFov, cue.amount, value * intensity); break;
                        case FeedbackKind.ScreenFlash: if (targetCanvas != null) targetCanvas.alpha = Mathf.LerpUnclamped(initialAlpha, cue.amount, value * intensity); break;
                        case FeedbackKind.PositionSpring:
                        {
                            Vector3 goal = startPosition + cue.vector * value * intensity;
                            Spring(ref springVelocity, ref springPosition, goal, cue.springStrength, cue.damping);
                            target.localPosition = springPosition; break;
                        }
                        case FeedbackKind.ScaleSpring:
                        case FeedbackKind.SquashStretchSpring:
                        {
                            Vector3 multiplier = cue.kind == FeedbackKind.SquashStretchSpring
                                ? new Vector3(1f + cue.vector.x * value, 1f - cue.vector.x * value * .7f, 1f + cue.vector.x * value)
                                : Vector3.LerpUnclamped(Vector3.one, cue.vectorB, value);
                            Vector3 goal = Vector3.Scale(startScale, multiplier);
                            Spring(ref springVelocity, ref springScale, goal, cue.springStrength, cue.damping); target.localScale = springScale; break;
                        }
                        case FeedbackKind.RotationSpring:
                        {
                            Vector3 goal = startRotation.eulerAngles + cue.vector * value * intensity;
                            Spring(ref springVelocity, ref springEuler, goal, cue.springStrength, cue.damping); target.localRotation = Quaternion.Euler(springEuler); break;
                        }
                        case FeedbackKind.URPBloom:
                        case FeedbackKind.URPChromaticAberration:
                        case FeedbackKind.URPLensDistortion:
                        case FeedbackKind.URPVignette:
                        case FeedbackKind.URPColorAdjustments:
                            SetVolumeValue(cue.kind, Mathf.LerpUnclamped(initialVolumeValue, cue.amount, value * intensity), bloom, chromatic, lens, vignette, grading); break;
                    }
                }
                catch (MissingReferenceException) { targetWasDestroyed = true; }
                if (targetWasDestroyed) yield break;
                yield return null;
            }
            if (target != null && cue.kind == FeedbackKind.TransformShake) { target.localPosition = startPosition; target.localRotation = startRotation; }
            if (target != null && cue.restoreAfterPlay && cue.kind == FeedbackKind.Scale) target.localScale = startScale;
            if (target != null && cue.restoreAfterPlay && cue.kind == FeedbackKind.Position) target.localPosition = startPosition;
            if (target != null && cue.restoreAfterPlay && cue.kind == FeedbackKind.Rotation) target.localRotation = startRotation;
            if (cue.restoreAfterPlay && cue.kind == FeedbackKind.LightIntensity && targetLight != null) targetLight.intensity = startLight;
            if (cue.restoreAfterPlay && cue.kind == FeedbackKind.TimeScale) Time.timeScale = previousTimeScale;
            if (cue.restoreAfterPlay && cue.kind == FeedbackKind.CameraZoom && targetCamera != null) targetCamera.fieldOfView = initialFov;
            if (cue.restoreAfterPlay && cue.kind == FeedbackKind.ScreenFlash && targetCanvas != null) targetCanvas.alpha = initialAlpha;
            if (target != null && cue.restoreAfterPlay && (cue.kind is FeedbackKind.PositionSpring or FeedbackKind.RotationSpring or FeedbackKind.ScaleSpring or FeedbackKind.SquashStretchSpring))
            { target.localPosition = startPosition; target.localRotation = startRotation; target.localScale = startScale; }
            SetVolumeValue(cue.kind, initialVolumeValue, bloom, chromatic, lens, vignette, grading);
        }

        static IEnumerator Wait(float duration, FeedbackTimeMode mode)
        {
            float elapsed = 0f;
            while (elapsed < duration) { elapsed += mode == FeedbackTimeMode.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime; yield return null; }
        }

        static bool RequiresTransform(FeedbackKind kind) => kind is
            FeedbackKind.TransformShake or
            FeedbackKind.Scale or
            FeedbackKind.Position or
            FeedbackKind.Rotation or
            FeedbackKind.PositionSpring or
            FeedbackKind.RotationSpring or
            FeedbackKind.ScaleSpring or
            FeedbackKind.SquashStretchSpring;

        static IEnumerable<FeedbackCue> Reverse(List<FeedbackCue> source)
        { for (int i = source.Count - 1; i >= 0; i--) yield return source[i]; }

        List<FeedbackCue> BuildRuntimeSequence()
        {
            var result = new List<FeedbackCue>();
            if (sequence != null)
                foreach (FeedbackAsset asset in sequence.Feedbacks)
                    if (asset != null && asset.Cue != null) result.Add(asset.Cue);
            return result;
        }

        T Resolve<T>(string id) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            if (runtimeContext != null && runtimeContext.TryBinding(id, out UnityEngine.Object contextual))
            {
                if (contextual is T exactContext) return exactContext;
                if (typeof(T) == typeof(GameObject) && contextual is Component contextualComponent) return contextualComponent.gameObject as T;
                if (contextual is GameObject contextualObject) return contextualObject.GetComponent(typeof(T)) as T;
                if (contextual is Component componentContext) return componentContext.GetComponent(typeof(T)) as T;
            }
            foreach (FeedbackBinding binding in bindings)
            {
                if (binding == null || binding.id != id || binding.target == null) continue;
                if (binding.target is T exact) return exact;
                if (typeof(T) == typeof(GameObject) && binding.target is Component boundComponent) return boundComponent.gameObject as T;
                if (binding.target is GameObject go) return go.GetComponent(typeof(T)) as T;
                if (binding.target is Component component) return component.GetComponent(typeof(T)) as T;
            }
            return null;
        }

        Transform ResolveTransformTarget(FeedbackCue cue)
        {
            if (cue == null) return transform;
            Transform bound = Resolve<Transform>(cue.bindingId);
            if (bound != null) return bound;
            // Unity's destroyed-object references are non-null to C#'s ?? operator,
            // but compare equal to null through UnityEngine.Object. Check explicitly.
            if (cue.target != null) return cue.target;
            return transform != null ? transform : null;
        }

        static void Spring(ref Vector3 velocity, ref Vector3 current, Vector3 target, float strength, float damping)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .033f);
            velocity += (target - current) * Mathf.Max(.01f, strength) * dt;
            velocity *= Mathf.Pow(Mathf.Clamp01(damping), dt * 60f);
            current += velocity * dt;
        }

        static float GetVolumeValue(FeedbackKind kind, Bloom b, ChromaticAberration c, LensDistortion l, Vignette v, ColorAdjustments a) => kind switch
        {
            FeedbackKind.URPBloom when b != null => b.intensity.value,
            FeedbackKind.URPChromaticAberration when c != null => c.intensity.value,
            FeedbackKind.URPLensDistortion when l != null => l.intensity.value,
            FeedbackKind.URPVignette when v != null => v.intensity.value,
            FeedbackKind.URPColorAdjustments when a != null => a.postExposure.value,
            _ => 0f
        };

        static void SetVolumeValue(FeedbackKind kind, float value, Bloom b, ChromaticAberration c, LensDistortion l, Vignette v, ColorAdjustments a)
        {
            switch (kind)
            {
                case FeedbackKind.URPBloom when b != null: b.intensity.Override(value); break;
                case FeedbackKind.URPChromaticAberration when c != null: c.intensity.Override(value); break;
                case FeedbackKind.URPLensDistortion when l != null: l.intensity.Override(value); break;
                case FeedbackKind.URPVignette when v != null: v.intensity.Override(value); break;
                case FeedbackKind.URPColorAdjustments when a != null: a.postExposure.Override(value); break;
            }
        }
    }
}
