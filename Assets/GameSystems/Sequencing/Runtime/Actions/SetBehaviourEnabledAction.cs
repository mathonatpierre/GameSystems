using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
    public sealed class SetBehaviourEnabledAction : GameAction
    {
        [SerializeField, Tooltip("Behaviour whose enabled state is changed.")] Behaviour target;
        [SerializeField, Tooltip("Desired enabled state.")] bool enabled = true;
        public override string Summary => $"Set {(target != null ? target.GetType().Name : "missing behaviour")} enabled = {enabled.ToString().ToLowerInvariant()}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetBehaviourEnabledAction data = (SetBehaviourEnabledAction)Definition;
                if (data.target == null) { Fail("Missing Behaviour target."); return; }
                data.target.enabled = data.enabled;
            }
        }
    }
}
