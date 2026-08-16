using System;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Feedbacks.Actions
{
    [Serializable]
    public sealed class TweenLightIntensityAction : GameAction
    {
        [SerializeReference] GameObjectValue target = new SelfGameObjectValue();
        [SerializeField] float intensity = 1f;
        [SerializeField, Min(.001f)] float duration = .15f;
        [SerializeField] AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] bool restoreAfterPlay = true;
        public TweenLightIntensityAction() { }
        public TweenLightIntensityAction(GameObjectValue target, float intensity, float duration, AnimationCurve curve, bool restore)
        { this.target = target; this.intensity = intensity; this.duration = duration; this.curve = curve; restoreAfterPlay = restore; }
        public override string Summary => $"Tween Light intensity to {intensity:0.##}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            Light light; float start, elapsed; TweenLightIntensityAction Data => (TweenLightIntensityAction)Definition;
            protected override void OnEnter() { base.OnEnter(); light = FeedbackActionUtility.Resolve<Light>(Data.target, Context, true); if (light == null) { Fail("Missing Light."); return; } start = light.intensity; elapsed = 0; }
            protected override bool Tick(float deltaTime) { if (Failed) return true; elapsed += Time.unscaledDeltaTime; float t = Mathf.Clamp01(elapsed / Mathf.Max(.001f, Data.duration)); light.intensity = Mathf.LerpUnclamped(start, Data.intensity, Data.curve?.Evaluate(t) ?? t); return t >= 1f; }
            protected override void OnExit() { if (light != null && Data.restoreAfterPlay) light.intensity = start; base.OnExit(); }
        }
    }
}
