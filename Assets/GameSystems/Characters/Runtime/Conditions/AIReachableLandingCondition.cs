using System;
using GameSystems.Sequencing;

namespace GameSystems.Characters
{
    [Serializable]
    public sealed class AIReachableLandingCondition : GameCondition
    {
        public override string Summary => "AI has a reachable landing ahead";
        protected override bool OnEvaluate(in GameActionContext context) =>
            context.TryGet(out CharacterAIContext ai) && ai.Controller.HasReachableLandingAhead();
    }
}
