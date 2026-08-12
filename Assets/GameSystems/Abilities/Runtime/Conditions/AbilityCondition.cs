using System;
using GameSystems.Actions;

namespace GameSystems.Abilities.Embedded
{
    [Serializable]
    public abstract class AbilityCondition : GameCondition
    {
        public bool Evaluate(in AbilityEvaluationContext context)
        {
            bool result = EvaluateAbility(context);
            RecordDebugResult(result);
            return result;
        }
        protected abstract bool EvaluateAbility(in AbilityEvaluationContext context);
        protected sealed override bool OnEvaluate(in GameActionContext context) =>
            context.TryGet(out AbilityEvaluationContext evaluation) && EvaluateAbility(evaluation);
    }
}
