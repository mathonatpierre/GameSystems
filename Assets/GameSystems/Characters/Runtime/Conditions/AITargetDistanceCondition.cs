using System;
using GameSystems.Sequencing;
using UnityEngine;

using GameSystems.Characters;

namespace GameSystems.Characters.Conditions
{
    [Serializable]
    public sealed class AITargetDistanceCondition : GameCondition
    {
        [SerializeField, Min(0f)] float minimum;
        [SerializeField, Min(0f)] float maximum = 5f;
        public override string Summary => $"AI target distance in [{minimum:0.##}, {maximum:0.##}]";
        protected override bool OnEvaluate(in GameActionContext context) =>
            context.TryGet(out CharacterAIContext ai) && ai.Target != null &&
            ai.Distance >= minimum && ai.Distance <= maximum;
    }
}
