using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
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
}
