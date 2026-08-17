using GameSystems.Abilities;
using GameSystems.Sequencing;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Characters
{
    [Serializable]
        public sealed class AIAbilityAcceptedRecentlyCondition : GameCondition
        {
            [SerializeField] AbilityDefinition ability;
            [SerializeField, Min(.01f)] float maximumAge = .5f;

            public AIAbilityAcceptedRecentlyCondition() { }
            public AIAbilityAcceptedRecentlyCondition(AbilityDefinition value, float age = .5f)
            { ability = value; maximumAge = Mathf.Max(.01f, age); }

            public override string Summary =>
                $"AI accepted {(ability != null ? ability.name : "missing ability")} recently";

            protected override bool OnEvaluate(in GameActionContext context) =>
                context.TryGet(out CharacterAIContext ai) && ability != null &&
                (ai.Character.Abilities.WasAcceptedRecently(ability, maximumAge) ||
                 ai.Blackboard.WasAcceptedRecently(ability, maximumAge));
        }

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
        public sealed class AIAbilityActiveCondition : GameCondition
        {
            [SerializeField] AbilityDefinition ability;

            public AIAbilityActiveCondition() { }
            public AIAbilityActiveCondition(AbilityDefinition value) => ability = value;
            public override string Summary =>
                $"AI is running {(ability != null ? ability.name : "missing ability")}";

            protected override bool OnEvaluate(in GameActionContext context)
            {
                if (!context.TryGet(out CharacterAIContext ai) || ability == null) return false;
                IReadOnlyList<AbilityRuntime> active = ai.Character.Abilities.ActiveAbilities;
                for (int i = 0; i < active.Count; i++)
                    if (active[i].Definition == ability) return true;
                return false;
            }
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
    
            protected override bool OnEvaluate(in GameActionContext context)
            {
                if (!context.TryGet(out CharacterAIContext ai)) return false;
                float direction = ai.Controller.Definition != null
                    ? ai.Controller.Definition.TraversalDirection : 0f;
                if (Mathf.Approximately(direction, 0f)) direction = 1f;
                foreach (CharacterAbilityController candidate in CharacterAbilityRegistry.Controllers)
                {
                    if (candidate == null || candidate.gameObject == ai.Character.Owner ||
                        ai.Blackboard.Traversal.IsIgnoredTarget(candidate.transform) ||
                        !HasAbility(candidate, requiredAbility)) continue;
                    Vector3 delta = candidate.transform.position - ai.Character.Transform.position;
                    if (Mathf.Abs(delta.x) > horizontalDistance) continue;
                    if (!mustBeBelow && delta.x * direction <= 0f) continue;
                    if (mustBeBelow ? delta.y > -.05f :
                            Mathf.Abs(delta.y) > verticalTolerance) continue;
                    return true;
                }
                return false;
            }

            static bool HasAbility(CharacterAbilityController candidate,
                AbilityDefinition required)
            {
                if (required == null) return true;
                AbilitySet set = candidate.AbilitySet;
                if (set == null) return false;
                IReadOnlyList<AbilityDefinition> abilities = set.Abilities;
                for (int i = 0; i < abilities.Count; i++)
                    if (abilities[i] == required) return true;
                return false;
            }
        }

    [Serializable]
        public sealed class AICharacterRisingCondition : GameCondition
        {
            [SerializeField] float minimumVerticalSpeed = .1f;
            public AICharacterRisingCondition() { }
            public AICharacterRisingCondition(float speed) => minimumVerticalSpeed = speed;
            public override string Summary => $"AI character rises faster than {minimumVerticalSpeed:0.##}";
            protected override bool OnEvaluate(in GameActionContext context) =>
                context.TryGet(out CharacterAIContext ai) && ai.Character.Motor != null &&
                ai.Character.Motor.Result.Velocity.y > minimumVerticalSpeed;
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
        public sealed class AICharacterAirborneCondition : GameCondition
        {
            public override string Summary => "AI character is airborne";
            protected override bool OnEvaluate(in GameActionContext context) =>
                context.TryGet(out CharacterAIContext ai) &&
                ai.Character.Motor != null && !ai.Character.Motor.Result.Ground.IsGrounded;
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
        public sealed class AIJumpTrajectoryClearCondition : GameCondition
        {
            [SerializeField, Min(.1f)] float maximumWallDistance = 2.2f;
            [SerializeField, Min(.1f)] float probeHeight = .55f;
            [SerializeField, Range(.06f, .3f)] float sampleSpacing = .12f;
            [SerializeField, Min(0f)] float capsuleClearance = .025f;

            public AIJumpTrajectoryClearCondition() { }
            public AIJumpTrajectoryClearCondition(float wallDistance, float height = .55f,
                float spacing = .12f, float clearance = .025f)
            {
                maximumWallDistance = Mathf.Max(.1f, wallDistance);
                probeHeight = Mathf.Max(.1f, height);
                sampleSpacing = Mathf.Clamp(spacing, .06f, .3f);
                capsuleClearance = Mathf.Max(0f, clearance);
            }

            public override string Summary =>
                $"AI jump capsule has a clear trajectory to wall within {maximumWallDistance:0.##}m";

            protected override bool OnEvaluate(in GameActionContext context)
            {
                if (!context.TryGet(out CharacterAIContext ai)) return false;
                CharacterAbilityController abilities = ai.Character.Abilities;
                CharacterController body = ai.Character.Owner.GetComponent<CharacterController>();
                CharacterAIDefinition definition = ai.Controller.Definition;
                if (abilities == null || body == null || definition == null ||
                    !CharacterCapabilityResolver.TryResolve(abilities.AbilitySet, body.height,
                        body.radius, capsuleClearance, out CharacterMovementCapabilities caps) ||
                    !caps.HeldJumpTrajectory.IsValid) return false;

                float direction = Mathf.Sign(definition.TraversalDirection);
                if (Mathf.Approximately(direction, 0f)) direction = 1f;
                Vector3 origin = ai.Character.Transform.position + Vector3.up * probeHeight;
                RaycastHit[] wallHits = Physics.RaycastAll(origin, Vector3.right * direction,
                    maximumWallDistance, definition.LineOfSightMask,
                    QueryTriggerInteraction.Ignore);
                RaycastHit wall = default;
                for (int i = 0; i < wallHits.Length; i++)
                {
                    RaycastHit candidate = wallHits[i];
                    if (candidate.collider == null ||
                        candidate.collider.GetComponentInParent<CharacterAbilityController>() != null ||
                        Mathf.Abs(Vector3.Dot(candidate.normal, Vector3.up)) >= .3f ||
                        candidate.distance <= wall.distance) continue;
                    wall = candidate;
                }
                if (wall.collider == null) return false;

                float lastDistance = Mathf.Max(.1f,
                    wall.distance - body.radius * 2f - capsuleClearance);
                Collider support = ai.Character.Motor.Result.Ground.Collider;
                for (float distance = sampleSpacing; distance <= lastDistance;
                     distance += sampleSpacing)
                {
                    if (!caps.HeldJumpTrajectory.TryGetHeightAtDistance(distance, out float rise))
                        return false;
                    Vector3 root = ai.Character.Transform.position +
                                   Vector3.right * direction * distance + Vector3.up * rise;
                    Vector3 center = root + ai.Character.Transform.rotation * body.center;
                    float radius = Mathf.Max(.02f, body.radius - capsuleClearance);
                    float halfSegment = Mathf.Max(0f,
                        body.height * .5f - radius - capsuleClearance);
                    Collider[] overlaps = Physics.OverlapCapsule(
                        center - Vector3.up * halfSegment,
                        center + Vector3.up * halfSegment, radius,
                        definition.LineOfSightMask, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < overlaps.Length; i++)
                    {
                        Collider candidate = overlaps[i];
                        if (candidate == null || candidate == body || candidate == support ||
                            candidate.transform == ai.Character.Transform ||
                            candidate.transform.IsChildOf(ai.Character.Transform)) continue;
                        return false;
                    }
                }
                return true;
            }
        }

    [Serializable]
        public sealed class AIWallAheadCondition : GameCondition
        {
            [SerializeField, Min(.05f)] float distance = .75f;
            [SerializeField, Min(.1f)] float probeHeight = .55f;

            public AIWallAheadCondition() { }
            public AIWallAheadCondition(float wallDistance, float height = .55f)
            { distance = Mathf.Max(.05f, wallDistance); probeHeight = Mathf.Max(.1f, height); }

            public override string Summary => $"AI has a wall within {distance:0.##}m";

            protected override bool OnEvaluate(in GameActionContext context)
            {
                if (!context.TryGet(out CharacterAIContext ai) || ai.Controller.Definition == null)
                    return false;
                float direction = Mathf.Sign(ai.Controller.Definition.TraversalDirection);
                if (Mathf.Approximately(direction, 0f)) direction = 1f;
                Vector3 origin = ai.Character.Transform.position + Vector3.up * probeHeight;
                return Physics.Raycast(origin, Vector3.right * direction, distance,
                    ai.Controller.Definition.LineOfSightMask, QueryTriggerInteraction.Ignore);
            }
        }

    [Serializable]
        public sealed class AIWallTraversalCondition : GameCondition
        {
            [SerializeField, Min(.05f)] float distance = 1.25f;
            [SerializeField, Min(.1f)] float probeHeight = .55f;

            public AIWallTraversalCondition() { }
            public AIWallTraversalCondition(float wallDistance, float height = .55f)
            { distance = Mathf.Max(.05f, wallDistance); probeHeight = Mathf.Max(.1f, height); }

            public override string Summary => $"AI maintains wall traversal within {distance:0.##}m";

            protected override bool OnEvaluate(in GameActionContext context) =>
                context.TryGet(out CharacterAIContext ai) &&
                ai.Blackboard is { IsGrounded: false } blackboard &&
                (blackboard.HasNearbyWall || blackboard.Traversal.WallTraversalActive);
        }

}
