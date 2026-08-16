using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
        public sealed class GameObjectActiveCondition : GameCondition
        {
            [SerializeField, Tooltip("Optional explicit target. Uses the context owner when empty.")] GameObject target;
            [SerializeField, Tooltip("Expected active-in-hierarchy state.")] bool expected = true;
            public override string Summary => $"{(target != null ? target.name : "Owner")} active = {expected.ToString().ToLowerInvariant()}";
            protected override bool OnEvaluate(in GameActionContext context)
            {
                GameObject value = target != null ? target : GameActionContextUtility.OwnerGameObject(context);
                return value != null && value.activeInHierarchy == expected;
            }
        }

    [Serializable]
        public sealed class ObjectAssignedCondition : GameCondition
        {
            [SerializeField, Tooltip("Object reference checked for a valid Unity object.")] UnityEngine.Object target;
            [SerializeField, Tooltip("Expected assigned state.")] bool expected = true;
            public override string Summary => $"Object {(expected ? "is assigned" : "is missing")}: {(target != null ? target.name : "None")}";
            protected override bool OnEvaluate(in GameActionContext context) => (target != null) == expected;
        }
}
