using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
    public sealed class GameActionSequence
    {
        [SerializeField, Tooltip("Prevent this sequence from being evaluated or executed.")] bool disabled;
        [SerializeField] GameConditionMode conditionMode = GameConditionMode.All;
        [SerializeReference] GameCondition[] conditions;
        [SerializeReference] GameAction[] actions;

        public GameConditionMode ConditionMode => conditionMode;
        public bool Enabled => !disabled;
        public GameCondition[] Conditions => conditions ?? Array.Empty<GameCondition>();
        public GameAction[] Actions => actions ?? Array.Empty<GameAction>();
        public bool CanRun(in GameActionContext context) => Enabled && GameConditionEvaluator.Evaluate(Conditions, conditionMode, context);

        public GameActionRunner CreateRunner(in GameActionContext context)
        {
            GameActionRunner runner = new();
            runner.Initialize(Enabled ? Actions : Array.Empty<GameAction>(), context);
            return runner;
        }

        public void Configure(GameCondition[] requiredConditions, GameAction[] orderedActions,
            GameConditionMode mode = GameConditionMode.All)
        {
            conditionMode = mode;
            conditions = requiredConditions ?? Array.Empty<GameCondition>();
            actions = orderedActions ?? Array.Empty<GameAction>();
        }
    }
}
