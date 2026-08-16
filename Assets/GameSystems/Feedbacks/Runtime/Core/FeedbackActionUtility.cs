using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Feedbacks
{
    public static class FeedbackActionUtility
    {
        public static GameObjectValue Binding(string id) => string.IsNullOrWhiteSpace(id)
            ? new SelfGameObjectValue() : new FeedbackBindingGameObjectValue(id);

        public static T Resolve<T>(GameObjectValue source, in GameActionContext context,
            bool children = false) where T : Component
        {
            GameObject gameObject = source?.Get(context);
            return gameObject == null ? null : children
                ? gameObject.GetComponentInChildren<T>(true)
                : gameObject.GetComponent<T>() ?? gameObject.GetComponentInParent<T>(true);
        }

        public static float Intensity(in GameActionContext context) =>
            context.TryGet(out FeedbackRuntimeContext feedback) ? feedback.Intensity : 1f;
    }
}
