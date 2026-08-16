using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Feedbacks.Actions
{
    [Serializable]
    public sealed class SpawnPooledFeedbackAction : GameAction
    {
        [SerializeField] GameObject prefab;
        [SerializeField, Min(.01f)] float lifetime = .15f;
        public SpawnPooledFeedbackAction() { }
        public SpawnPooledFeedbackAction(GameObject prefab, float lifetime)
        { this.prefab = prefab; this.lifetime = lifetime; }
        public override string Summary => $"Spawn pooled {prefab?.name ?? "missing prefab"}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SpawnPooledFeedbackAction data = (SpawnPooledFeedbackAction)Definition;
                if (data.prefab == null) { Fail("Missing prefab."); return; }
                Vector3 position = Context.TryGet(out FeedbackRuntimeContext feedback)
                    ? feedback.Position : GameActionContextUtility.OwnerGameObject(Context)?.transform.position ?? Vector3.zero;
                Quaternion rotation = Context.TryGet(out feedback) ? feedback.Rotation : Quaternion.identity;
                FeedbackPool.Spawn(data.prefab, position, rotation, data.lifetime);
            }
        }
    }
}
