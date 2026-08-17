using System;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Feedbacks.Actions
{
    [Serializable]
    public sealed class AddCameraShakeAction : GameAction
    {
        [SerializeField] float amplitude = .1f;
        [SerializeField, Min(0f)] float duration = .1f;
        public AddCameraShakeAction() { }
        public AddCameraShakeAction(float amplitude, float duration)
        { this.amplitude = amplitude; this.duration = duration; }
        public override string Summary => $"Add camera shake {amplitude:0.###} for {duration:0.###}s";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                AddCameraShakeAction data = (AddCameraShakeAction)Definition;
                MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Exclude);
                for (int i = 0; i < behaviours.Length; i++)
                    if (behaviours[i] is ICameraShakeReceiver receiver)
                    { receiver.AddImpactShake(data.amplitude * FeedbackActionUtility.Intensity(Context), data.duration); return; }
                Fail("Missing ICameraShakeReceiver.");
            }
        }
    }

    [Serializable]
    public sealed class TweenCameraFieldOfViewAction : GameAction
    {
        [SerializeReference] GameObjectValue target = new SelfGameObjectValue();
        [SerializeField] float fieldOfView = 40f;
        [SerializeField, Min(.001f)] float duration = .2f;
        [SerializeField] AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] bool restoreAfterPlay = true;
        public TweenCameraFieldOfViewAction() { }
        public TweenCameraFieldOfViewAction(GameObjectValue target, float value, float duration,
            AnimationCurve curve, bool restore)
        { this.target = target; fieldOfView = value; this.duration = duration; this.curve = curve; restoreAfterPlay = restore; }
        public override string Summary => $"Tween Camera field of view to {fieldOfView:0.##}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            Camera camera; float start; float elapsed;
            TweenCameraFieldOfViewAction Data => (TweenCameraFieldOfViewAction)Definition;
            protected override void OnEnter()
            { base.OnEnter(); camera = FeedbackActionUtility.Resolve<Camera>(Data.target, Context, true) ?? Camera.main; if (camera == null) { Fail("Missing Camera."); return; } start = camera.fieldOfView; elapsed = 0; }
            protected override bool Tick(float deltaTime)
            { if (Failed) return true; elapsed += Time.unscaledDeltaTime; float t = Mathf.Clamp01(elapsed / Mathf.Max(.001f, Data.duration)); camera.fieldOfView = Mathf.LerpUnclamped(start, Data.fieldOfView, Data.curve?.Evaluate(t) ?? t); return t >= 1f; }
            protected override void OnExit() { if (camera != null && Data.restoreAfterPlay) camera.fieldOfView = start; base.OnExit(); }
        }
    }
}
