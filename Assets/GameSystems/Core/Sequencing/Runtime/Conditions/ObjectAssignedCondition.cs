using System;
using UnityEngine;

namespace GameSystems.Actions
{
    [Serializable]
    public sealed class ObjectAssignedCondition : GameCondition
    {
        [SerializeField, Tooltip("Object reference checked for a valid Unity object.")] UnityEngine.Object target;
        [SerializeField, Tooltip("Expected assigned state.")] bool expected = true;
        public override string Summary => $"Object {(expected ? "is assigned" : "is missing")}: {(target != null ? target.name : "None")}";
        protected override bool OnEvaluate(in GameActionContext context) => (target != null) == expected;
    }
}
