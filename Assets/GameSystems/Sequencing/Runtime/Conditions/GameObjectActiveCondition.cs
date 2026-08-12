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
}
