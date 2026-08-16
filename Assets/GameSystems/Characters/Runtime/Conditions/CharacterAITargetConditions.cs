using GameSystems.Characters;
using GameSystems.Sequencing;
using System;
using UnityEngine;

namespace GameSystems.Characters.Conditions
{
    [Serializable]
        public sealed class AILineOfSightCondition : GameCondition
        {
            [SerializeField] bool expected = true;
            public override string Summary => expected ? "AI has line of sight" : "AI line of sight blocked";
            protected override bool OnEvaluate(in GameActionContext context) =>
                context.TryGet(out CharacterAIContext ai) && ai.HasLineOfSight == expected;
        }

    [Serializable]
        public sealed class HasAITargetCondition : GameCondition
        {
            [SerializeField] bool expected = true;
            public override string Summary => expected ? "Has AI target" : "Has no AI target";
            protected override bool OnEvaluate(in GameActionContext context) =>
                context.TryGet(out CharacterAIContext ai) && (ai.Target != null) == expected;
        }
}
