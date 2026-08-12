using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Abilities.Conditions
{
    [Serializable]
    public sealed class AILineOfSightCondition : GameCondition
    {
        [SerializeField] bool expected = true;
        public override string Summary => expected ? "AI has line of sight" : "AI line of sight blocked";
        protected override bool OnEvaluate(in GameActionContext context) =>
            context.TryGet(out CharacterAIContext ai) && ai.HasLineOfSight == expected;
    }
}
