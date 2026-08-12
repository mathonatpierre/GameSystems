using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
    public sealed class DestroyObjectAction : GameAction
    {
        [SerializeField, Tooltip("Explicit object destroyed by this action.")] UnityEngine.Object target;
        [SerializeField, Min(0f), Tooltip("Delay passed to Unity Object.Destroy.")] float delay;
        public override string Summary => $"Destroy {(target != null ? target.name : "missing object")} after {delay:0.###}s";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                DestroyObjectAction data = (DestroyObjectAction)Definition;
                if (data.target == null) { Fail("Missing object to destroy."); return; }
                UnityEngine.Object.Destroy(data.target, data.delay);
            }
        }
    }
}
