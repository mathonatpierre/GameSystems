using System;
using GameSystems.Actions;
using UnityEngine;

namespace GameSystems.Abilities.Conditions
{
    [Serializable]
    public sealed class HasAITargetCondition : GameCondition
    {
        [SerializeField] bool expected = true;
        public override string Summary => expected ? "Has AI target" : "Has no AI target";
        protected override bool OnEvaluate(in GameActionContext context) =>
            context.TryGet(out CharacterAIContext ai) && (ai.Target != null) == expected;
    }
}
