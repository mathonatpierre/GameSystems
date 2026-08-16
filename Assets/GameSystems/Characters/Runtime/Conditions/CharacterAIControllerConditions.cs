using GameSystems.Abilities;
using GameSystems.Sequencing;
using System;
using UnityEngine;

namespace GameSystems.Characters
{
    [Serializable]
        public sealed class AIAbilityCanStartCondition : GameCondition
        {
            [SerializeField] AbilityDefinition ability;

            public AIAbilityCanStartCondition() { }
            public AIAbilityCanStartCondition(AbilityDefinition value) => ability = value;
            public override string Summary =>
                $"AI can start {(ability != null ? ability.name : "missing ability")}";

            protected override bool OnEvaluate(in GameActionContext context) =>
                context.TryGet(out CharacterAIContext ai) &&
                ai.Character.Abilities != null && ai.Character.Abilities.CanStart(ability, ai.Controller);
        }

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

    [Serializable]
        public sealed class AICharacterGroundedCondition : GameCondition
        {
            public override string Summary => "AI character is grounded";
            protected override bool OnEvaluate(in GameActionContext context) =>
                context.TryGet(out CharacterAIContext ai) &&
                ai.Character.Motor != null && ai.Character.Motor.Result.Ground.IsGrounded;
        }

    [Serializable]
        public sealed class AIGroundAheadCondition : GameCondition
        {
            [SerializeField, Min(.05f)] float forwardDistance = .65f;
            [SerializeField, Min(.05f)] float probeHeight = .35f;
            [SerializeField, Min(.1f)] float probeDistance = 1.2f;
            [SerializeField] bool expected = true;
    
            public AIGroundAheadCondition() { }
            public AIGroundAheadCondition(bool hasGround, float distance = .65f)
            { expected = hasGround; forwardDistance = distance; }
    
            public override string Summary => expected ? "AI has ground ahead" : "AI has a gap ahead";
    
            protected override bool OnEvaluate(in GameActionContext context)
            {
                if (!context.TryGet(out CharacterAIContext ai)) return false;
                float direction = Mathf.Abs(ai.Direction.x) > .01f ? Mathf.Sign(ai.Direction.x) : 1f;
                Vector3 origin = ai.Character.Transform.position +
                                 Vector3.right * direction * forwardDistance + Vector3.up * probeHeight;
                bool found = Physics.Raycast(origin, Vector3.down, probeDistance,
                    ai.Controller.Definition.LineOfSightMask, QueryTriggerInteraction.Ignore);
                return found == expected;
            }
        }

    [Serializable]
        public sealed class AIReachableLandingCondition : GameCondition
        {
            public override string Summary => "AI has a reachable landing ahead";
            protected override bool OnEvaluate(in GameActionContext context) =>
                context.TryGet(out CharacterAIContext ai) && ai.Controller.HasReachableLandingAhead();
        }
}
