using System;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Sequencing
{
    public enum TransformTweenEasing { Linear, SmoothStep }

    [Serializable]
    public sealed class SetTransformPositionAction : GameAction
    {
        [SerializeReference] GameObjectValue target = new SelfGameObjectValue();
        [SerializeReference] Vector3Value position = new ConstantVector3Value();
        [SerializeField, Min(0f)] float duration;
        [SerializeField] TransformTweenEasing easing = TransformTweenEasing.SmoothStep;
        public override string Summary => $"Set {target?.Summary} position to {position?.Summary}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            Transform transform;
            Vector3 start;
            Vector3 end;
            float elapsed;
            SetTransformPositionAction Data => (SetTransformPositionAction)Definition;
            protected internal override void OnEnter()
            {
                base.OnEnter();
                transform = Data.target?.Get(Context)?.transform;
                if (transform == null) { Fail("Missing transform target."); return; }
                start = transform.position;
                end = Data.position?.Get(Context) ?? start;
                if (Data.duration <= 0f) transform.position = end;
            }
            protected internal override bool Tick(float deltaTime)
            {
                if (Failed || Data.duration <= 0f) return true;
                elapsed += deltaTime;
                float t = Mathf.Clamp01(elapsed / Data.duration);
                if (Data.easing == TransformTweenEasing.SmoothStep) t = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.LerpUnclamped(start, end, t);
                return elapsed >= Data.duration;
            }
        }
    }

    [Serializable]
    public sealed class SetTransformRotationAction : GameAction
    {
        [SerializeReference] GameObjectValue target = new SelfGameObjectValue();
        [SerializeReference] QuaternionValue rotation = new ConstantEulerRotationValue();
        [SerializeField, Min(0f)] float duration;
        [SerializeField] TransformTweenEasing easing = TransformTweenEasing.SmoothStep;
        public override string Summary => $"Set {target?.Summary} rotation to {rotation?.Summary}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            Transform transform;
            Quaternion start;
            Quaternion end;
            float elapsed;
            SetTransformRotationAction Data => (SetTransformRotationAction)Definition;
            protected internal override void OnEnter()
            {
                base.OnEnter();
                transform = Data.target?.Get(Context)?.transform;
                if (transform == null) { Fail("Missing transform target."); return; }
                start = transform.rotation;
                end = Data.rotation?.Get(Context) ?? start;
                if (Data.duration <= 0f) transform.rotation = end;
            }
            protected internal override bool Tick(float deltaTime)
            {
                if (Failed || Data.duration <= 0f) return true;
                elapsed += deltaTime;
                float t = Mathf.Clamp01(elapsed / Data.duration);
                if (Data.easing == TransformTweenEasing.SmoothStep) t = Mathf.SmoothStep(0f, 1f, t);
                transform.rotation = Quaternion.SlerpUnclamped(start, end, t);
                return elapsed >= Data.duration;
            }
        }
    }

    [Serializable]
    public sealed class SetTransformParentAction : GameAction
    {
        [SerializeReference] GameObjectValue target = new SelfGameObjectValue();
        [SerializeReference] GameObjectValue parent = new TargetGameObjectValue();
        [SerializeField] bool worldPositionStays = true;
        public override string Summary => $"Set {target?.Summary} parent to {parent?.Summary}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetTransformParentAction data = (SetTransformParentAction)Definition;
                Transform target = data.target?.Get(Context)?.transform;
                if (target == null) { Fail("Missing transform target."); return; }
                target.SetParent(data.parent?.Get(Context)?.transform, data.worldPositionStays);
            }
        }
    }
}
