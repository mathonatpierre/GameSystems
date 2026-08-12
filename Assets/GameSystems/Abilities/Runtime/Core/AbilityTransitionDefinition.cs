using System;
using GameSystems.Sequencing;
using UnityEngine;
namespace GameSystems.Abilities
{
    [Serializable]
    public sealed class AbilityTransitionDefinition
    {
        [SerializeField] string label = "Transition";
        [SerializeField] AbilityTransitionTrigger trigger;
        [SerializeField] AbilityDefinition target;
        [SerializeField] int priority;
        [SerializeField] bool completeSource = true;
        [SerializeField] GameActionSequence sequence = new();
        public string Label => label;
        public AbilityTransitionTrigger Trigger => trigger;
        public AbilityDefinition Target => target;
        public int Priority => priority;
        public bool CompleteSource => completeSource;
        public GameActionSequence Sequence => sequence ??= new GameActionSequence();
        public GameCondition[] Conditions => Sequence.Conditions;
        public GameAction[] Actions => Sequence.Actions;
        public bool Evaluate(in AbilityEvaluationContext context)
        {
            return Sequence.CanRun(CreateActionContext(context));
        }
        public GameActionRunner ExecuteActions(in AbilityEvaluationContext context)
        {
            GameActionRunner runner = Sequence.CreateRunner(CreateActionContext(context));
            runner.Start();
            return runner;
        }

        static GameActionContext CreateActionContext(in AbilityEvaluationContext context) =>
            new(context.Character.Owner, context, context.Character, context.Runtime);
    }
}
