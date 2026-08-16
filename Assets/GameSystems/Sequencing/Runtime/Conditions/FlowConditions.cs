using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
        public sealed class AlwaysCondition : GameCondition
        {
            [SerializeField, Tooltip("Constant result returned by this condition.")] bool result = true;
            public override string Summary => result ? "Always true" : "Always false";
            protected override bool OnEvaluate(in GameActionContext context) => result;
        }

    [Serializable]
        public sealed class GameConditionGroup : GameCondition
        {
            [SerializeField, Tooltip("How the child condition results are combined.")]
            GameConditionMode mode = GameConditionMode.All;
            [SerializeReference, Tooltip("Inline conditions evaluated as one group.")]
            GameCondition[] conditions;
    
            public GameConditionMode Mode => mode;
            public GameCondition[] Conditions => conditions ?? Array.Empty<GameCondition>();
            public override string Summary => $"{mode} of {Conditions.Length} conditions";
    
            protected override bool OnEvaluate(in GameActionContext context) =>
                GameConditionEvaluator.Evaluate(Conditions, mode, context);
        }

    [Serializable]
        public sealed class RandomChanceCondition : GameCondition
        {
            [SerializeField, Range(0f, 1f), Tooltip("Probability of returning true for each evaluation.")] float probability = .5f;
            public override string Summary => $"Random chance {probability * 100f:0.#}%";
            protected override bool OnEvaluate(in GameActionContext context) => UnityEngine.Random.value <= probability;
        }
}
