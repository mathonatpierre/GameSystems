using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GameSystems.Playables
{
    [DefaultExecutionOrder(30000)]
    [DisallowMultipleComponent]
    public sealed class UnityPlayableAnimationPlayer : MonoBehaviour
    {
        sealed class Entry
        {
            public PlayableAnimationRuntime Runtime;
            public int Input = -1;
            public float Weight;
        }

        [SerializeField] Animator animator;
        [SerializeField] bool autoFindAnimator = true;
        readonly Dictionary<PlayableAnimationAsset, Entry> entries = new();
        readonly PlayableAnimationContext context = new();
        PlayableGraph graph;
        AnimationMixerPlayable rootMixer;
        PlayableAnimationAsset current;
        IPlayablePostProcessor[] postProcessors;
        float currentBlendDuration = .08f;
        float currentFacingOffset;

        public PlayableAnimationContext Context => context;
        public PlayableAnimationAsset Current => current;
        public int EvaluationCount { get; private set; }
        public float CurrentWeight => current != null && entries.TryGetValue(current, out Entry entry) ? entry.Weight : 0f;

        public void Configure(Animator value, bool findAutomatically = true)
        {
            bool outputChanged = animator != value;
            animator = value;
            autoFindAnimator = findAutomatically;
            if (outputChanged && graph.IsValid())
            {
                PlayableAnimationAsset selected = current;
                graph.Destroy();
                entries.Clear();
                rootMixer = default;
                current = null;
                EnsureGraph();
                if (selected != null) Play(selected, currentBlendDuration, true);
                return;
            }
            if (isActiveAndEnabled) EnsureGraph();
        }

        void Awake()
        {
            if (animator == null && autoFindAnimator) animator = GetComponentInChildren<Animator>();
            EnsureGraph();
        }

        public void Play(PlayableAnimationAsset asset, float blendDuration = -1f, bool forceRestart = false)
        {
            if (asset == null) return;
            EnsureGraph();
            Entry entry = GetOrCreate(asset);
            bool changed = current != asset;
            current = asset;
            currentBlendDuration = blendDuration >= 0f ? blendDuration : asset.DefaultBlendDuration;
            if (changed || forceRestart || asset.RestartWhenPlayed) entry.Runtime.Restart();
        }

        void EnsureGraph()
        {
            if (graph.IsValid()) return;
            graph = PlayableGraph.Create($"{name} Unity Playables");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            if (animator != null)
            {
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                rootMixer = AnimationMixerPlayable.Create(graph, 0);
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
                output.SetSourcePlayable(rootMixer);
            }
            context.Configure(gameObject, GetComponent<PlayableAnimationBindings>(), animator != null);
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            var processors = new List<IPlayablePostProcessor>();
            for (int i = 0; i < behaviours.Length; i++)
                if (behaviours[i] is IPlayablePostProcessor processor) processors.Add(processor);
            postProcessors = processors.ToArray();
            if (animator != null) graph.Play();
        }

        Entry GetOrCreate(PlayableAnimationAsset asset)
        {
            if (entries.TryGetValue(asset, out Entry found)) return found;
            PlayableAnimationRuntime runtime = asset.CreateRuntime(graph);
            var entry = new Entry { Runtime = runtime };
            if (rootMixer.IsValid() && runtime.Playable.IsValid())
            {
                entry.Input = rootMixer.GetInputCount();
                rootMixer.SetInputCount(entry.Input + 1);
                graph.Connect(runtime.Playable, 0, rootMixer, entry.Input);
            }
            entries.Add(asset, entry);
            return entry;
        }

        // Animator/Animation Playables have produced their pose before this additive pass.
        void LateUpdate() => EvaluateNow();

        public void EvaluateNow()
        {
            if (!graph.IsValid() || current == null || !entries.TryGetValue(current, out Entry selected)) return;
            EvaluationCount++;
            float blend = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(.001f, currentBlendDuration));
            currentFacingOffset = Mathf.LerpAngle(currentFacingOffset, current.FacingOffset, blend);
            context.SetFloat("PlayableFacingOffset", currentFacingOffset);
            foreach (Entry entry in entries.Values)
            {
                float target = entry == selected ? 1f : 0f;
                entry.Weight = Mathf.Lerp(entry.Weight, target, blend);
                if (entry.Input >= 0) rootMixer.SetInputWeight(entry.Input, entry.Weight);
                context.SetFloat("PlayableWeight", entry.Weight);
                entry.Runtime.Evaluate(context);
            }
            for (int i = 0; i < postProcessors.Length; i++)
                postProcessors[i]?.ApplyPlayablePostProcess();
        }

        void OnDisable()
        {
            if (graph.IsValid()) graph.Destroy();
            entries.Clear(); current = null; currentFacingOffset = 0f;
        }
    }
}
