using System;
using System.Collections.Generic;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Feedbacks
{
    public sealed class FeedbackRuntimeContext
    {
        readonly Dictionary<string, UnityEngine.Object> bindings = new();

        public float Intensity { get; set; } = 1f;
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; } = Quaternion.identity;
        public Vector3 Normal { get; set; } = Vector3.up;

        public void Bind(string id, UnityEngine.Object value)
        {
            if (!string.IsNullOrWhiteSpace(id)) bindings[id] = value;
        }

        public UnityEngine.Object Resolve(string id) =>
            !string.IsNullOrWhiteSpace(id) && bindings.TryGetValue(id, out UnityEngine.Object value)
                ? value : null;
    }

    [Serializable]
    public sealed class FeedbackBindingGameObjectValue : GameObjectValue
    {
        [SerializeField] string bindingId;

        public FeedbackBindingGameObjectValue() { }
        public FeedbackBindingGameObjectValue(string bindingId) => this.bindingId = bindingId;
        public override string Summary => $"Feedback binding [{bindingId}]";

        public override GameObject Get(in GameActionContext context)
        {
            if (!context.TryGet(out FeedbackRuntimeContext feedback)) return null;
            UnityEngine.Object value = feedback.Resolve(bindingId);
            return value switch
            {
                GameObject gameObject => gameObject,
                Component component => component.gameObject,
                _ => null
            };
        }
    }

    [Serializable]
    public sealed class FeedbackIntensityFloatValue : FloatValue
    {
        public override string Summary => "Feedback intensity";
        public override float Get(in GameActionContext context) =>
            context.TryGet(out FeedbackRuntimeContext feedback) ? feedback.Intensity : 1f;
    }

    [Serializable]
    public sealed class FeedbackPositionVector3Value : Vector3Value
    {
        public override string Summary => "Feedback position";
        public override Vector3 Get(in GameActionContext context) =>
            context.TryGet(out FeedbackRuntimeContext feedback) ? feedback.Position : Vector3.zero;
    }
}
