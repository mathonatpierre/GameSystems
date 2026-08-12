using System;
using UnityEngine;

namespace GameSystems.Abilities.Embedded
{
    [Serializable]
    public sealed class FallDistanceCondition : AbilityCondition
    {
        [SerializeField, Min(.01f), Tooltip("Minimum vertical distance from the ability start position.")]
        float minimumDistance = 7.5f;

        public FallDistanceCondition() { }
        public FallDistanceCondition(float minimumDistance = 7.5f) => this.minimumDistance = minimumDistance;

        public override string Summary => $"Fall distance >= {minimumDistance:0.##}m";

        protected override bool EvaluateAbility(in AbilityEvaluationContext context) =>
            context.Runtime != null && context.Character.Transform.position.y <=
            context.Runtime.StartPosition.y - minimumDistance;
    }
}
