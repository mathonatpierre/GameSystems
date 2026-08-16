using System;
using UnityEngine;

namespace GameSystems.Sequencing.Values
{
    [Serializable]
        public abstract class BoolValue : GameValue
        {
            public abstract bool Get(in GameActionContext context);
        }
    
        [Serializable]
        public sealed class ConstantBoolValue : BoolValue
        {
            [SerializeField] bool value;
            public override string Summary => value.ToString();
            public override bool Get(in GameActionContext context) => value;
        }

    [Serializable]
        public abstract class FloatValue : GameValue
        {
            public abstract float Get(in GameActionContext context);
        }
    
        [Serializable]
        public sealed class ConstantFloatValue : FloatValue
        {
            [SerializeField] float value;
            public ConstantFloatValue() { }
            public ConstantFloatValue(float value) => this.value = value;
            public override string Summary => value.ToString("0.###");
            public override float Get(in GameActionContext context) => value;
        }
    
        [Serializable]
        public sealed class FloatVariableValue : FloatValue
        {
            [SerializeField] string key;
            public override string Summary => $"[{key}]";
            public override float Get(in GameActionContext context) =>
                context.TryGet(out GameActionBlackboard variables) &&
                variables.TryGet(key, out float value) ? value : 0f;
        }

    [Serializable]
        public abstract class QuaternionValue : GameValue
        {
            public abstract Quaternion Get(in GameActionContext context);
        }
    
        [Serializable]
        public sealed class ConstantEulerRotationValue : QuaternionValue
        {
            [SerializeField] Vector3 eulerAngles;
            public override string Summary => eulerAngles.ToString("0.##");
            public override Quaternion Get(in GameActionContext context) => Quaternion.Euler(eulerAngles);
        }
    
        [Serializable]
        public sealed class TransformRotationValue : QuaternionValue
        {
            [SerializeReference] GameObjectValue source = new SelfGameObjectValue();
            public TransformRotationValue() { }
            public TransformRotationValue(GameObjectValue source) => this.source = source;
            public override string Summary => $"{source?.Summary ?? "None"} rotation";
            public override Quaternion Get(in GameActionContext context) =>
                source?.Get(context)?.transform.rotation ?? Quaternion.identity;
        }

    [Serializable]
        public abstract class Vector3Value : GameValue
        {
            public abstract Vector3 Get(in GameActionContext context);
        }
    
        [Serializable]
        public sealed class ConstantVector3Value : Vector3Value
        {
            [SerializeField] Vector3 value;
            public ConstantVector3Value() { }
            public ConstantVector3Value(Vector3 value) => this.value = value;
            public override string Summary => value.ToString("0.##");
            public override Vector3 Get(in GameActionContext context) => value;
        }
    
        [Serializable]
        public sealed class TransformPositionValue : Vector3Value
        {
            [SerializeReference] GameObjectValue source = new SelfGameObjectValue();
            [SerializeField] Vector3 offset;
            [SerializeField] Space space = Space.World;
            public TransformPositionValue() { }
            public TransformPositionValue(GameObjectValue source, Vector3 offset = default,
                Space space = Space.World)
            { this.source = source; this.offset = offset; this.space = space; }
            public override string Summary => $"{source?.Summary ?? "None"} position";
            public override Vector3 Get(in GameActionContext context)
            {
                Transform transform = source?.Get(context)?.transform;
                if (transform == null) return offset;
                return space == Space.Self ? transform.TransformPoint(offset) : transform.position + offset;
            }
        }
    
        [Serializable]
        public sealed class DirectionBetweenObjectsValue : Vector3Value
        {
            [SerializeReference] GameObjectValue from = new SelfGameObjectValue();
            [SerializeReference] GameObjectValue to = new TargetGameObjectValue();
            [SerializeField] bool normalize = true;
            public override string Summary => $"Direction {from?.Summary} to {to?.Summary}";
            public override Vector3 Get(in GameActionContext context)
            {
                GameObject source = from?.Get(context);
                GameObject destination = to?.Get(context);
                Vector3 value = source != null && destination != null
                    ? destination.transform.position - source.transform.position : Vector3.zero;
                return normalize && value.sqrMagnitude > .0001f ? value.normalized : value;
            }
        }
    
        [Serializable]
        public sealed class Vector3VariableValue : Vector3Value
        {
            [SerializeField] string key;
            public override string Summary => $"[{key}]";
            public override Vector3 Get(in GameActionContext context) =>
                context.TryGet(out GameActionBlackboard variables) &&
                variables.TryGet(key, out Vector3 value) ? value : Vector3.zero;
        }
    
        [Serializable]
        public sealed class ScaleVector3Value : Vector3Value
        {
            [SerializeReference] Vector3Value value = new ConstantVector3Value();
            [SerializeReference] FloatValue multiplier = new ConstantFloatValue(1f);
            public override string Summary => $"{value?.Summary} x {multiplier?.Summary}";
            public override Vector3 Get(in GameActionContext context) =>
                (value?.Get(context) ?? Vector3.zero) * (multiplier?.Get(context) ?? 0f);
        }
    
        [Serializable]
        public sealed class AddVector3Values : Vector3Value
        {
            [SerializeReference] Vector3Value left = new ConstantVector3Value();
            [SerializeReference] Vector3Value right = new ConstantVector3Value();
            public override string Summary => $"{left?.Summary} + {right?.Summary}";
            public override Vector3 Get(in GameActionContext context) =>
                (left?.Get(context) ?? Vector3.zero) + (right?.Get(context) ?? Vector3.zero);
        }
}
