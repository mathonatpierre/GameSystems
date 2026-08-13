using System;
using GameSystems.Sequencing;
using UnityEngine;
using GameSystems.Abilities;

namespace GameSystems.Characters
{
    [Serializable]
    public sealed class AICharacterAheadCondition : GameCondition
    {
        [SerializeField, Min(.1f)] float horizontalDistance = 2.2f;
        [SerializeField, Min(.1f)] float verticalTolerance = 1.1f;
        [SerializeField] bool mustBeBelow;
        [SerializeField] AbilityDefinition requiredAbility;

        public AICharacterAheadCondition() { }
        public AICharacterAheadCondition(float distance, float verticalRange, bool below = false,
            AbilityDefinition ability = null)
        { horizontalDistance = distance; verticalTolerance = verticalRange; mustBeBelow = below;
            requiredAbility = ability; }

        public override string Summary => mustBeBelow
            ? $"AI has a character below within {horizontalDistance:0.#}m"
            : $"AI has a character ahead within {horizontalDistance:0.#}m";

        protected override bool OnEvaluate(in GameActionContext context) =>
            context.TryGet(out CharacterAIContext ai) &&
            ai.Controller.HasCharacterAhead(horizontalDistance, verticalTolerance, mustBeBelow,
                requiredAbility);
    }
}
