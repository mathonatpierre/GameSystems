using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
        public sealed class BehaviourEnabledCondition : GameCondition
        {
            [SerializeField, Tooltip("Behaviour inspected by this condition.")] Behaviour target;
            [SerializeField, Tooltip("Expected enabled state.")] bool expected = true;
            public override string Summary => $"{(target != null ? target.GetType().Name : "Missing behaviour")} enabled = {expected.ToString().ToLowerInvariant()}";
            protected override bool OnEvaluate(in GameActionContext context) => target != null && target.enabled == expected;
        }
}
