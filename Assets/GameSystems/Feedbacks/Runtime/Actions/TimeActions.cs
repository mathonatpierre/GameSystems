using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Feedbacks.Actions
{
    [Serializable]
    public sealed class FreezeTimeAction : GameAction
    {
        [SerializeField, Range(0f, 1f)] float timeScale = .05f;
        [SerializeField, Min(0f)] float duration = .05f;
        public FreezeTimeAction() { }
        public FreezeTimeAction(float timeScale, float duration)
        { this.timeScale = timeScale; this.duration = duration; }
        public override string Summary => $"Freeze time at {timeScale:0.##} for {duration:0.###}s";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            float elapsed; FeedbackTime.FreezeLease lease; FreezeTimeAction Data => (FreezeTimeAction)Definition;
            protected override void OnEnter() { base.OnEnter(); elapsed = 0; lease = FeedbackTime.Acquire(this, Data.timeScale); }
            protected override bool Tick(float deltaTime) { elapsed += Time.unscaledDeltaTime; return elapsed >= Data.duration; }
            protected override void OnExit() { FeedbackTime.Release(lease); lease = null; base.OnExit(); }
        }
    }

    [Serializable]
    public sealed class TweenTimeScaleAction : GameAction
    {
        [SerializeField] float timeScale = 1f;
        [SerializeField, Min(.001f)] float duration = .15f;
        [SerializeField] AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] bool restoreAfterPlay = true;
        public TweenTimeScaleAction() { }
        public TweenTimeScaleAction(float value, float duration, AnimationCurve curve, bool restore)
        { timeScale = value; this.duration = duration; this.curve = curve; restoreAfterPlay = restore; }
        public override string Summary => $"Tween time scale to {timeScale:0.##}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            float start, elapsed; TweenTimeScaleAction Data => (TweenTimeScaleAction)Definition;
            protected override void OnEnter() { base.OnEnter(); start = Time.timeScale; elapsed = 0; }
            protected override bool Tick(float deltaTime) { elapsed += Time.unscaledDeltaTime; float t = Mathf.Clamp01(elapsed / Mathf.Max(.001f, Data.duration)); Time.timeScale = Mathf.LerpUnclamped(start, Data.timeScale, Data.curve?.Evaluate(t) ?? t); return t >= 1f; }
            protected override void OnExit() { if (Data.restoreAfterPlay) Time.timeScale = start; base.OnExit(); }
        }
    }
}
