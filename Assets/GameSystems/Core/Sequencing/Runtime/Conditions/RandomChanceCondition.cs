using System;
using UnityEngine;

namespace GameSystems.Actions
{
    [Serializable]
    public sealed class RandomChanceCondition : GameCondition
    {
        [SerializeField, Range(0f, 1f), Tooltip("Probability of returning true for each evaluation.")] float probability = .5f;
        public override string Summary => $"Random chance {probability * 100f:0.#}%";
        protected override bool OnEvaluate(in GameActionContext context) => UnityEngine.Random.value <= probability;
    }
}
