using System;
using System.Collections.Generic;
using GameSystems.Abilities;
using GameSystems.Characters.AI;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Characters.Actions
{
    [Serializable]
    public sealed class SetAIProgressionInputAction : GameAction
    {
        public override string Summary => "Set AI horizontal input to progression direction";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                CharacterAIContext ai = Context.Get<CharacterAIContext>();
                float direction = ai.Controller.Definition != null
                    ? ai.Controller.Definition.TraversalDirection : 0f;
                if (ai.Blackboard.IsGrounded)
                {
                    ai.Blackboard.Traversal.WallTraversalActive = false;
                    ai.Blackboard.Traversal.WallTraversalDirection = 0f;
                    ai.Blackboard.Traversal.ClearLanding();
                }
                ai.Blackboard.SetHorizontalIntent(Mathf.Approximately(direction, 0f) ? 1f : direction);
            }
        }
    }

    [Serializable]
    public sealed class SetAIWallTraversalInputAction : GameAction
    {
        public override string Summary => "Set AI horizontal input to wall traversal direction";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                CharacterAIContext ai = Context.Get<CharacterAIContext>();
                float direction = ai.Blackboard.Traversal.WallTraversalDirection;
                if (Mathf.Approximately(direction, 0f))
                { Fail("Missing wall traversal direction."); return; }
                ai.Blackboard.SetHorizontalIntent(direction);
            }
        }
    }

    [Serializable]
    public sealed class SetAIPlannedLandingInputAction : GameAction
    {
        public override string Summary => "Steer AI horizontal input to planned landing";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                CharacterAIContext ai = Context.Get<CharacterAIContext>();
                CharacterAIBlackboard.TraversalState traversal = ai.Blackboard.Traversal;
                if (!traversal.TryGetLandingX(ai.Character.Abilities, out float landingX))
                { Fail("Missing planned landing."); return; }
                float correction = landingX - ai.Character.Transform.position.x;
                if (Mathf.Abs(correction) < .06f)
                { ai.Blackboard.SetHorizontalIntent(0f); return; }
                bool overshot = !Mathf.Approximately(traversal.LandingDirection, 0f) &&
                                correction * traversal.LandingDirection < 0f;
                float intent = overshot && traversal.LandingIsDynamicTarget ? 0f :
                    overshot ? Mathf.Clamp(correction * .45f, -.35f, .35f) :
                    Mathf.Clamp(correction * 2.1f, -1f, 1f);
                ai.Blackboard.SetHorizontalIntent(intent);
            }
        }
    }

    [Serializable]
    public sealed class QueueAIAbilityRequestAction : GameAction
    {
        [SerializeField] AbilityDefinition ability;

        public QueueAIAbilityRequestAction() { }
        public QueueAIAbilityRequestAction(AbilityDefinition value) => ability = value;

        public override string Summary =>
            $"Queue AI ability {(ability != null ? ability.name : "missing")}";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                QueueAIAbilityRequestAction data = (QueueAIAbilityRequestAction)Definition;
                if (data.ability == null) { Fail("Missing ability."); return; }
                if (!Context.TryGet(out CharacterBehaviorTreeRuntime runtime))
                { Fail("Missing behavior tree runtime."); return; }
                runtime.Request(data.ability);
            }
        }
    }

    [Serializable]
    public sealed class PlanAICharacterLandingAction : GameAction
    {
        [SerializeField] AbilityDefinition requiredAbility;
        [SerializeField, Min(.1f)] float horizontalDistance = 2.2f;
        [SerializeField, Min(.1f)] float verticalTolerance = 1.1f;

        public PlanAICharacterLandingAction() { }
        public PlanAICharacterLandingAction(AbilityDefinition ability, float distance,
            float verticalRange)
        {
            requiredAbility = ability;
            horizontalDistance = Mathf.Max(.1f, distance);
            verticalTolerance = Mathf.Max(.1f, verticalRange);
        }

        public override string Summary => requiredAbility != null
            ? $"Plan AI landing on character with {requiredAbility.name}"
            : "Plan AI landing on character";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                PlanAICharacterLandingAction data = (PlanAICharacterLandingAction)Definition;
                CharacterAIContext ai = Context.Get<CharacterAIContext>();
                if (!TryPlan(ai, data.requiredAbility, data.horizontalDistance,
                        data.verticalTolerance))
                { Fail("No reachable character landing."); return; }
                float targetX = ai.Blackboard.Traversal.LandingTarget != null
                    ? ai.Blackboard.Traversal.LandingTarget.position.x
                    : ai.Character.Transform.position.x;
                ai.Blackboard.SetHorizontalIntent(Mathf.Clamp(
                    (targetX - ai.Character.Transform.position.x) * 2.1f, -1f, 1f));
            }

            static bool TryPlan(in CharacterAIContext ai, AbilityDefinition requiredAbility,
                float horizontalDistance, float verticalTolerance)
            {
                float direction = ai.Controller.Definition != null
                    ? ai.Controller.Definition.TraversalDirection : 0f;
                if (Mathf.Approximately(direction, 0f)) direction = 1f;
                CharacterAbilityController best = null;
                float bestDistance = float.MaxValue;
                foreach (CharacterAbilityController candidate in CharacterAbilityRegistry.Controllers)
                {
                    if (candidate == null || candidate.gameObject == ai.Character.Owner ||
                        ai.Blackboard.Traversal.IsIgnoredTarget(candidate.transform) ||
                        !HasAbility(candidate, requiredAbility)) continue;
                    Vector3 delta = candidate.transform.position - ai.Character.Transform.position;
                    if (delta.x * direction <= 0f || Mathf.Abs(delta.x) > horizontalDistance ||
                        Mathf.Abs(delta.y) > verticalTolerance) continue;
                    float score = Mathf.Abs(delta.x) + Mathf.Abs(delta.y) * .2f;
                    if (score >= bestDistance) continue;
                    best = candidate;
                    bestDistance = score;
                }
                if (best == null || !TryCapabilities(ai, out CharacterMovementCapabilities caps,
                        out float initialSpeed)) return false;
                Collider collider = best.GetComponent<Collider>();
                Vector3 point = collider != null
                    ? new Vector3(collider.bounds.center.x, collider.bounds.max.y - .06f,
                        collider.bounds.center.z) : best.transform.position;
                float gap = Mathf.Abs(point.x - ai.Character.Transform.position.x);
                float rise = point.y - ai.Character.Transform.position.y;
                if (!CharacterTraversalSolver.TryCalibrateRuntimeJump(caps, gap, rise,
                        CharacterTraversalSolver.StandardJumpSafety, initialSpeed * direction,
                        out float hold, out float flight)) return false;
                ai.Blackboard.Traversal.SetLanding(best.transform, collider, null, point,
                    hold, flight, ai.Character.Transform.position.x);
                return true;
            }

            static bool HasAbility(CharacterAbilityController candidate, AbilityDefinition required)
            {
                if (required == null) return true;
                AbilitySet set = candidate.AbilitySet;
                if (set == null) return false;
                IReadOnlyList<AbilityDefinition> abilities = set.Abilities;
                for (int i = 0; i < abilities.Count; i++)
                    if (abilities[i] == required) return true;
                return false;
            }

            static bool TryCapabilities(in CharacterAIContext ai,
                out CharacterMovementCapabilities capabilities, out float speed)
            {
                CharacterController body = ai.Character.Owner.GetComponent<CharacterController>();
                speed = ai.Character.Motor?.Result.Velocity.x ?? 0f;
                capabilities = default;
                return CharacterCapabilityResolver.TryResolve(ai.Character.Abilities.AbilitySet,
                    body != null ? body.height : 1.1f, body != null ? body.radius : .18f,
                    .2f, out capabilities);
            }
        }
    }

    [Serializable]
    public sealed class PlanAIReachableLandingAction : GameAction
    {
        public override string Summary => "Plan AI reachable landing along progression";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                CharacterAIContext ai = Context.Get<CharacterAIContext>();
                float direction = ai.Controller.Definition != null
                    ? ai.Controller.Definition.TraversalDirection : 0f;
                if (Mathf.Approximately(direction, 0f)) direction = 1f;
                if (!TryPlan(ai, direction))
                { Fail("No reachable landing along progression."); return; }
                if (ai.Blackboard.Traversal.TryGetLandingX(ai.Character.Abilities,
                        out float landingX))
                    ai.Blackboard.SetHorizontalIntent(Mathf.Clamp(
                        (landingX - ai.Character.Transform.position.x) * 2.1f, -1f, 1f));
            }

            static bool TryPlan(in CharacterAIContext ai, float direction)
            {
                CharacterAIDefinition definition = ai.Controller.Definition;
                CharacterAITraversalSettings settings = definition.Traversal;
                CharacterController body = ai.Character.Owner.GetComponent<CharacterController>();
                if (!CharacterCapabilityResolver.TryResolve(ai.Character.Abilities.AbilitySet,
                        body != null ? body.height : 1.1f,
                        body != null ? body.radius : .18f, .2f,
                        out CharacterMovementCapabilities caps)) return false;
                float initialSpeed = ai.Character.Motor.Result.Velocity.x * direction;
                for (float distance = settings.EdgeProbeDistance;
                     distance <= settings.MaximumJumpReach; distance += .22f)
                {
                    Vector3 origin = ai.Character.Transform.position +
                        Vector3.right * direction * distance + Vector3.up * 1.1f;
                    if (!RaycastGround(ai, origin, settings.LandingProbeDepth,
                            out RaycastHit landing)) continue;
                    float rise = landing.point.y - ai.Character.Transform.position.y;
                    if (!CharacterTraversalSolver.TryCalibrateRuntimeJump(caps, distance, rise,
                            CharacterTraversalSolver.StandardJumpSafety, initialSpeed,
                            out float hold, out float flight)) continue;
                    ICharacterMovingPlatform moving = landing.collider.GetComponentInParent(
                        typeof(ICharacterMovingPlatform)) as ICharacterMovingPlatform;
                    Vector3 point = landing.point;
                    if (moving != null)
                    {
                        Bounds bounds = landing.collider.bounds;
                        float futureCenter = bounds.center.x + moving.PredictDisplacement(flight).x;
                        if (Mathf.Abs(origin.x - futureCenter) >
                            Mathf.Max(.12f, bounds.extents.x - .18f)) continue;
                        point = new Vector3(futureCenter, bounds.max.y, bounds.center.z);
                    }
                    ai.Blackboard.Traversal.SetLanding(null, landing.collider, moving, point,
                        hold, flight, ai.Character.Transform.position.x);
                    return true;
                }
                return TryArrivingPlatform(ai, direction, caps, initialSpeed);
            }

            static bool TryArrivingPlatform(in CharacterAIContext ai, float direction,
                in CharacterMovementCapabilities caps, float initialSpeed)
            {
                CharacterAITraversalSettings settings = ai.Controller.Definition.Traversal;
                Vector3 center = ai.Character.Transform.position + Vector3.right * direction *
                    ((settings.EdgeProbeDistance + settings.MaximumJumpReach) * .5f);
                Vector3 half = new((settings.MaximumJumpReach - settings.EdgeProbeDistance) * .65f,
                    settings.LandingProbeDepth * .5f, 1.5f);
                Collider[] candidates = Physics.OverlapBox(center, half, Quaternion.identity,
                    ai.Controller.Definition.LineOfSightMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < candidates.Length; i++)
                {
                    Collider collider = candidates[i];
                    ICharacterMovingPlatform moving = collider.GetComponentInParent(
                        typeof(ICharacterMovingPlatform)) as ICharacterMovingPlatform;
                    if (moving == null) continue;
                    Bounds bounds = collider.bounds;
                    float flight = CharacterTraversalSolver.TimeToTravelDistance(
                        Mathf.Abs(bounds.center.x - ai.Character.Transform.position.x),
                        initialSpeed, caps.AirSpeed, caps.AirAcceleration);
                    float futureCenter = bounds.center.x;
                    float hold = 0f;
                    for (int iteration = 0; iteration < 3; iteration++)
                    {
                        futureCenter = bounds.center.x + moving.PredictDisplacement(flight).x;
                        if (!CharacterTraversalSolver.TryCalibrateRuntimeJump(caps,
                                Mathf.Abs(futureCenter - ai.Character.Transform.position.x),
                                bounds.max.y - ai.Character.Transform.position.y,
                                CharacterTraversalSolver.StandardJumpSafety, initialSpeed,
                                out hold, out flight))
                        { flight = -1f; break; }
                    }
                    float delta = (futureCenter - ai.Character.Transform.position.x) * direction;
                    if (flight < 0f || delta < settings.EdgeProbeDistance ||
                        delta > settings.MaximumJumpReach) continue;
                    ai.Blackboard.Traversal.SetLanding(null, collider, moving,
                        new Vector3(futureCenter, bounds.max.y, bounds.center.z), hold, flight,
                        ai.Character.Transform.position.x);
                    return true;
                }
                return false;
            }

            static bool RaycastGround(in CharacterAIContext ai, Vector3 origin, float distance,
                out RaycastHit valid)
            {
                RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance,
                    ai.Controller.Definition.LineOfSightMask, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform == ai.Character.Transform ||
                        hits[i].transform.IsChildOf(ai.Character.Transform)) continue;
                    valid = hits[i];
                    return true;
                }
                valid = default;
                return false;
            }
        }
    }

    [Serializable]
    public sealed class BeginAIWallTraversalAction : GameAction
    {
        public override string Summary => "Begin AI wall traversal from nearby wall";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                CharacterAIContext ai = Context.Get<CharacterAIContext>();
                if (!ai.Blackboard.HasNearbyWall ||
                    Mathf.Approximately(ai.Blackboard.NearbyWallDirection, 0f))
                { Fail("No nearby wall to begin traversal."); return; }
                CharacterAIBlackboard.TraversalState traversal = ai.Blackboard.Traversal;
                traversal.WallTraversalActive = true;
                traversal.WallTraversalDirection = ai.Blackboard.NearbyWallDirection;
                traversal.ClearLanding();
                ai.Blackboard.SetHorizontalIntent(traversal.WallTraversalDirection);
            }
        }
    }

    [Serializable]
    public sealed class ContinueAIWallTraversalAction : GameAction
    {
        public override string Summary => "Continue AI away from contacted wall";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                CharacterAIContext ai = Context.Get<CharacterAIContext>();
                float normal = ai.Character.Motor.Result.Wall.Normal.x;
                if (Mathf.Abs(normal) <= .01f)
                { Fail("No horizontal wall contact."); return; }
                ai.Blackboard.Traversal.WallTraversalActive = true;
                ai.Blackboard.Traversal.WallTraversalDirection = Mathf.Sign(normal);
                ai.Blackboard.SetHorizontalIntent(ai.Blackboard.Traversal.WallTraversalDirection);
            }
        }
    }

    [Serializable]
    public sealed class SetAIAbilityHoldDurationAction : GameAction
    {
        [SerializeField, Min(0f)] float duration = .25f;

        public SetAIAbilityHoldDurationAction() { }
        public SetAIAbilityHoldDurationAction(float value) => duration = Mathf.Max(0f, value);

        public override string Summary => $"Set AI ability hold to {duration:0.###} seconds";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetAIAbilityHoldDurationAction data = (SetAIAbilityHoldDurationAction)Definition;
                Context.Get<CharacterAIContext>().Blackboard.Traversal.PlanAbilityHold(data.duration);
            }
        }
    }

}
