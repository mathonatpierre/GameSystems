using System;
using UnityEngine;
namespace GameSystems.Sequencing
{
    [Serializable]
    public sealed class DelayAction : GameAction
    {
        [SerializeField, Min(0f), Tooltip("Time to wait before continuing the sequence.")] float duration = .1f;
        [SerializeField, Tooltip("Ignore the gameplay time scale while waiting.")] bool useUnscaledTime;
        public float Duration => duration;
        public bool UseUnscaledTime => useUnscaledTime;
        public DelayAction() { }
        public DelayAction(float duration, bool useUnscaledTime = false)
        { this.duration = Mathf.Max(0f, duration); this.useUnscaledTime = useUnscaledTime; }
        public override string Summary => $"Wait {duration:0.###}s, unscaled = {useUnscaledTime.ToString().ToLowerInvariant()}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            float elapsed; DelayAction Data => (DelayAction)Definition;
            protected internal override void OnEnter() { base.OnEnter(); elapsed = 0f; }
            protected internal override bool Tick(float deltaTime) { elapsed += Data.UseUnscaledTime ? Time.unscaledDeltaTime : deltaTime; return elapsed >= Data.Duration; }
        }
    }
}
