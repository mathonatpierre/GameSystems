using System;
using GameSystems.Sequencing;

namespace GameSystems.Characters
{
    [Serializable]
    public sealed class AICharacterGroundedCondition : GameCondition
    {
        public override string Summary => "AI character is grounded";
        protected override bool OnEvaluate(in GameActionContext context) =>
            context.TryGet(out CharacterAIContext ai) &&
            ai.Character.Motor != null && ai.Character.Motor.Result.Ground.IsGrounded;
    }
}
