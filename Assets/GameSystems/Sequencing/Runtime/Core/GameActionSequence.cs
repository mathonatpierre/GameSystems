using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
    public sealed class GameActionSequence
    {
        [SerializeField] GameConditionMode conditionMode = GameConditionMode.All;
        [SerializeReference] GameCondition[] conditions;
        [SerializeReference] GameAction[] actions;

        public GameConditionMode ConditionMode => conditionMode;
        public GameCondition[] Conditions => conditions ?? Array.Empty<GameCondition>();
        public GameAction[] Actions => actions ?? Array.Empty<GameAction>();
        public bool CanRun(in GameActionContext context) => GameConditionEvaluator.Evaluate(Conditions, conditionMode, context);

        public GameActionRunner CreateRunner(in GameActionContext context)
        {
            GameActionRunner runner = new();
            runner.Initialize(Actions, context);
            return runner;
        }
    }
}
