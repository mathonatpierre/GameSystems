using UnityEngine;
using GameSystems.Hooks;
using GameSystems.Abilities;
using UnityEngine.Scripting.APIUpdating;
using System.Collections.Generic;
using GameSystems.Abilities.Actions;
using GameSystems.Characters.AI;

namespace GameSystems.Characters
{
    [MovedFrom(true, "GameSystems.Abilities", "GameSystems.Abilities", "CharacterAIController")]
    [DisallowMultipleComponent, DefaultExecutionOrder(-400)]
    public sealed class CharacterAIController : MonoBehaviour, ICharacterCommandSource,
        IHorizontalInputProvider, IAbilityInputState, IAbilityRequestObserver, ICharacterTargetProvider
    {
        [SerializeField] CharacterAIDefinition definition;
        [SerializeField, Tooltip("Optional fixed target. Without one, the nearest other ability controller is selected.")]
        Transform targetOverride;

        Transform currentTarget;
        double nextDecisionAt;
        readonly CharacterRequestBuffer pending = new();
        Transform plannedLandingTarget;
        Collider plannedLandingCollider;
        ICharacterMovingPlatform plannedMovingPlatform;
        Vector3 plannedLandingPoint;
        float plannedHoldDuration;
        float plannedFlightTime;
        double planCreatedAt;
        double holdUntil;
        AbilityDefinition heldAbility;
        bool wasAirborne;
        float previousVerticalVelocity;
        double suppressJumpRequestsUntil;
        CharacterBehaviorTreeRuntime behaviorRuntime;

        public CharacterAIDefinition Definition => definition;
        public Transform CurrentTarget => currentTarget;
        public CharacterAIDecision CurrentDecision { get; private set; }
        public CharacterAIContext LastContext { get; private set; }
        public CharacterBehaviorTreeRuntime BehaviorRuntime => behaviorRuntime;
        public float Horizontal
        {
            get
            {
                if (currentTarget == null) return 0f;
                if (!IsGrounded() && TryGetPlannedLandingX(out float landingX))
                {
                    float correction = landingX - transform.position.x;
                    return Mathf.Abs(correction) < .06f ? 0f : Mathf.Clamp(correction * 2.1f, -1f, 1f);
                }
                if (!IsGrounded() && TryGetAirborneCharacterTarget(out Transform airborneTarget))
                {
                    float correction = airborneTarget.position.x - transform.position.x;
                    return Mathf.Abs(correction) < .08f ? 0f : Mathf.Clamp(correction * 1.8f, -1f, 1f);
                }
                float direction = Mathf.Sign(currentTarget.position.x - transform.position.x);
                return ShouldWaitAtEdge(direction) ? 0f : direction;
            }
        }

        public void Configure(CharacterAIDefinition value)
        {
            definition = value;
            behaviorRuntime = definition?.BehaviorTree?.CreateRuntime();
        }
        public void SetTarget(Transform value) => targetOverride = value;

        public bool HasReachableLandingAhead()
        {
            if (definition == null || currentTarget == null) return false;
            float direction = Mathf.Sign(currentTarget.position.x - transform.position.x);
            return TryFindLanding(direction, out _);
        }

        public bool HasCharacterAhead(float horizontalDistance, float verticalTolerance,
            bool mustBeBelow, AbilityDefinition requiredAbility = null)
        {
            float direction = currentTarget == null ? 1f :
                Mathf.Sign(currentTarget.position.x - transform.position.x);
            foreach (CharacterAbilityController candidate in CharacterAbilityRegistry.Controllers)
            {
                if (candidate == null || candidate.gameObject == gameObject) continue;
                if (!HasAbility(candidate, requiredAbility)) continue;
                Vector3 delta = candidate.transform.position - transform.position;
                if (Mathf.Abs(delta.x) > horizontalDistance) continue;
                if (!mustBeBelow && delta.x * direction <= 0f) continue;
                if (mustBeBelow ? delta.y > -.05f : Mathf.Abs(delta.y) > verticalTolerance) continue;
                if (!mustBeBelow && Mathf.Abs(delta.y) > verticalTolerance) continue;
                if (!mustBeBelow && requiredAbility != null)
                {
                    Collider targetCollider = candidate.GetComponent<Collider>();
                    Vector3 landingPoint = targetCollider != null
                        ? new Vector3(targetCollider.bounds.center.x, targetCollider.bounds.max.y,
                            targetCollider.bounds.center.z)
                        : candidate.transform.position;
                    if (!TryPlanLanding(candidate.transform,
                            landingPoint,
                            targetCollider, null, direction)) continue;
                }
                return true;
            }
            return false;
        }

        public void CollectCommands(CharacterRuntimeContext context, CharacterRequestBuffer requests)
        {
            if (definition == null || Time.timeAsDouble < nextDecisionAt) return;
            nextDecisionAt = Time.timeAsDouble + definition.DecisionInterval;
            currentTarget = ResolveTarget(context);
            LastContext = new CharacterAIContext(context, this, currentTarget,
                CheckLineOfSight(context, currentTarget));

            if (definition.BehaviorTree != null)
            {
                behaviorRuntime ??= definition.BehaviorTree.CreateRuntime();
                behaviorRuntime.Tick(LastContext, Time.timeAsDouble);
                AbilityDefinition requested = behaviorRuntime.RequestedAbility;
                if (requested == null || IsGrounded() &&
                    Time.timeAsDouble < suppressJumpRequestsUntil) return;
                requests.Add(new AbilityRequest(requested, this, 1f, Time.timeAsDouble));
                return;
            }

            CharacterAIDecision winner = null;
            CharacterAIDecision[] decisions = definition.Decisions;
            CharacterAIDecision[] ordered = (CharacterAIDecision[])decisions.Clone();
            System.Array.Sort(ordered, (left, right) =>
                (right?.Priority ?? int.MinValue).CompareTo(left?.Priority ?? int.MinValue));
            for (int i = 0; i < ordered.Length; i++)
            {
                CharacterAIDecision decision = ordered[i];
                if (decision == null) continue;
                decision.ClearDebug();
                if (!decision.Evaluate(LastContext, Time.timeAsDouble)) continue;
                winner = decision;
                break;
            }
            if (CurrentDecision != null && CurrentDecision != winner)
                context.Abilities.Cancel(CurrentDecision.Ability);
            CurrentDecision = winner;
            if (winner == null) return;
            winner.MarkSelected(Time.timeAsDouble);
            if (!IsGrounded() || Time.timeAsDouble < suppressJumpRequestsUntil) return;
            requests.Add(new AbilityRequest(winner.Ability, this, 1f, Time.timeAsDouble));
        }

        void Update()
        {
            CharacterAbilityController abilities = GetComponent<CharacterAbilityController>();
            if (abilities == null) return;
            bool airborne = abilities.Motor != null && !abilities.Motor.Result.Ground.IsGrounded;
            float verticalVelocity = abilities.Motor?.Result.Velocity.y ?? 0f;
            bool bounced = airborne && IsLastAcceptedBounce(abilities) && verticalVelocity > 2f &&
                           verticalVelocity - previousVerticalVelocity > 2.5f;
            if (bounced) HandleAirborneBounce(abilities);
            if (wasAirborne && !airborne) ClearTraversalPlan();
            wasAirborne = airborne;
            previousVerticalVelocity = verticalVelocity;
            pending.Clear();
            CollectCommands(abilities.Context, pending);
            for (int i = 0; i < pending.Requests.Count; i++) abilities.Submit(pending.Requests[i]);
        }

        public bool AnyAbilityHeld => heldAbility != null && Time.timeAsDouble < holdUntil;

        public bool IsHeld(AbilityDefinition ability) =>
            ability != null && ability == heldAbility && Time.timeAsDouble < holdUntil;

        public void OnAbilityRequestResolved(in AbilityRequest request, AbilityRequestResult result)
        {
            if (result != AbilityRequestResult.Accepted || plannedHoldDuration <= 0f ||
                Time.timeAsDouble - planCreatedAt > .3d || !IsGrounded()) return;
            heldAbility = request.Ability;
            holdUntil = Time.timeAsDouble + plannedHoldDuration;
        }

        Transform ResolveTarget(CharacterRuntimeContext context)
        {
            if (targetOverride != null) return targetOverride;
            if (definition.TargetHook != null)
                return HookRegistry.Get(definition.TargetHook)?.transform;
            Transform best = null;
            float bestDistance = definition.DetectionRadius;
            foreach (CharacterAbilityController candidate in CharacterAbilityRegistry.Controllers)
            {
                if (candidate == null || candidate.gameObject == context.Owner) continue;
                float distance = Vector3.Distance(context.Transform.position, candidate.transform.position);
                if (distance > bestDistance) continue;
                best = candidate.transform;
                bestDistance = distance;
            }
            return best;
        }

        bool CheckLineOfSight(CharacterRuntimeContext context, Transform target)
        {
            if (target == null) return false;
            Vector3 origin = context.Transform.position;
            Vector3 destination = target.position;
            Vector3 delta = destination - origin;
            if (delta.sqrMagnitude < .0001f) return true;
            if (!Physics.Raycast(origin, delta.normalized, out RaycastHit hit, delta.magnitude,
                    definition.LineOfSightMask, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        bool ShouldWaitAtEdge(float direction)
        {
            if (definition == null || !IsGrounded()) return false;
            CharacterAITraversalSettings settings = definition.Traversal;
            Vector3 edgeOrigin = transform.position + Vector3.up * .35f +
                                 Vector3.right * direction * settings.EdgeProbeDistance;
            if (RaycastGround(edgeOrigin, 1.25f, out _)) return false;
            return !TryFindLanding(direction, out _);
        }

        bool TryFindLanding(float direction, out RaycastHit landing)
        {
            CharacterAITraversalSettings settings = definition.Traversal;
            if (!TryGetTraversalCapabilities(out CharacterMovementCapabilities capabilities,
                    out float initialSpeed))
            { landing = default; return false; }
            initialSpeed *= direction;
            for (float distance = settings.EdgeProbeDistance; distance <= settings.MaximumJumpReach;
                 distance += .22f)
            {
                Vector3 origin = transform.position + Vector3.right * direction * distance + Vector3.up * 1.1f;
                if (!RaycastGround(origin, settings.LandingProbeDepth, out landing)) continue;
                ICharacterMovingPlatform moving = landing.collider.GetComponentInParent(
                    typeof(ICharacterMovingPlatform)) as ICharacterMovingPlatform;
                float rise = landing.point.y - transform.position.y;
                if (!CharacterTraversalSolver.TryCalibrateRuntimeJump(capabilities, distance, rise,
                        CharacterTraversalSolver.StandardJumpSafety, initialSpeed,
                        out float holdDuration, out float flightTime)) continue;
                if (moving == null)
                {
                    SetTraversalPlan(null, landing.collider, null, landing.point,
                        holdDuration, flightTime);
                    return true;
                }
                Bounds bounds = landing.collider.bounds;
                float futureCenter = bounds.center.x + moving.PredictDisplacement(flightTime).x;
                if (Mathf.Abs(origin.x - futureCenter) <= Mathf.Max(.12f, bounds.extents.x - .18f))
                {
                    SetTraversalPlan(null, landing.collider, moving,
                        new Vector3(futureCenter, bounds.max.y, bounds.center.z), holdDuration, flightTime);
                    return true;
                }
            }
            if (TryFindArrivingMovingPlatform(direction, capabilities, initialSpeed, out landing)) return true;
            landing = default;
            return false;
        }

        bool TryFindArrivingMovingPlatform(float direction,
            in CharacterMovementCapabilities capabilities, float initialSpeed, out RaycastHit landing)
        {
            CharacterAITraversalSettings settings = definition.Traversal;
            Vector3 center = transform.position + Vector3.right * direction *
                ((settings.EdgeProbeDistance + settings.MaximumJumpReach) * .5f);
            Vector3 half = new((settings.MaximumJumpReach - settings.EdgeProbeDistance) * .65f,
                settings.LandingProbeDepth * .5f, 1.5f);
            Collider[] candidates = Physics.OverlapBox(center, half, Quaternion.identity,
                definition.LineOfSightMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < candidates.Length; i++)
            {
                Collider collider = candidates[i];
                ICharacterMovingPlatform moving = collider.GetComponentInParent(
                    typeof(ICharacterMovingPlatform)) as ICharacterMovingPlatform;
                if (moving == null) continue;
                Bounds bounds = collider.bounds;
                float flightTime = CharacterTraversalSolver.TimeToTravelDistance(
                    Mathf.Abs(bounds.center.x - transform.position.x), initialSpeed,
                    capabilities.AirSpeed, capabilities.AirAcceleration);
                float futureCenter = bounds.center.x;
                float holdDuration = 0f;
                for (int iteration = 0; iteration < 3; iteration++)
                {
                    futureCenter = bounds.center.x + moving.PredictDisplacement(flightTime).x;
                    float predictedGap = Mathf.Abs(futureCenter - transform.position.x);
                    float rise = bounds.max.y - transform.position.y;
                    if (!CharacterTraversalSolver.TryCalibrateRuntimeJump(capabilities, predictedGap,
                            rise, CharacterTraversalSolver.StandardJumpSafety, initialSpeed,
                            out holdDuration, out flightTime))
                    { flightTime = -1f; break; }
                }
                if (flightTime < 0f) continue;
                float delta = (futureCenter - transform.position.x) * direction;
                if (delta < settings.EdgeProbeDistance || delta > settings.MaximumJumpReach) continue;
                SetTraversalPlan(null, collider, moving,
                    new Vector3(futureCenter, bounds.max.y, bounds.center.z), holdDuration, flightTime);
                landing = default;
                return true;
            }
            landing = default;
            return false;
        }

        bool TryGetTraversalCapabilities(out CharacterMovementCapabilities capabilities,
            out float signedInitialSpeed)
        {
            CharacterAbilityController abilities = GetComponent<CharacterAbilityController>();
            signedInitialSpeed = abilities?.Motor?.Result.Velocity.x ?? 0f;
            capabilities = default;
            return abilities != null && CharacterCapabilityResolver.TryResolve(abilities.AbilitySet,
                GetComponent<CharacterController>()?.height ?? 1.1f,
                GetComponent<CharacterController>()?.radius ?? .18f, .2f, out capabilities);
        }

        bool TryPlanLanding(Transform target, Vector3 point, Collider collider,
            ICharacterMovingPlatform moving, float direction)
        {
            if (!TryGetTraversalCapabilities(out CharacterMovementCapabilities capabilities,
                    out float initialSpeed)) return false;
            float gap = Mathf.Abs(point.x - transform.position.x);
            float rise = point.y - transform.position.y;
            if (!CharacterTraversalSolver.TryCalibrateRuntimeJump(capabilities, gap, rise,
                    CharacterTraversalSolver.StandardJumpSafety, initialSpeed * direction,
                    out float hold, out float flight)) return false;
            SetTraversalPlan(target, collider, moving, point, hold, flight);
            return true;
        }

        void SetTraversalPlan(Transform target, Collider collider, ICharacterMovingPlatform moving,
            Vector3 point, float holdDuration, float flightTime)
        {
            plannedLandingTarget = target;
            plannedLandingCollider = collider;
            plannedMovingPlatform = moving;
            plannedLandingPoint = point;
            plannedHoldDuration = holdDuration;
            plannedFlightTime = flightTime;
            planCreatedAt = Time.timeAsDouble;
        }

        bool TryGetPlannedLandingX(out float worldX)
        {
            if (plannedLandingTarget != null)
            {
                worldX = plannedLandingTarget.position.x;
                return true;
            }
            if (plannedLandingCollider != null)
            {
                if (plannedMovingPlatform != null)
                {
                    CharacterAbilityController abilities = GetComponent<CharacterAbilityController>();
                    float remaining = Mathf.Max(0f, plannedFlightTime - (abilities?.Motor?.Result.AirTime ?? 0f));
                    worldX = plannedLandingCollider.bounds.center.x +
                             plannedMovingPlatform.PredictDisplacement(remaining).x;
                }
                else worldX = plannedLandingPoint.x;
                return true;
            }
            worldX = 0f;
            return false;
        }

        void ClearTraversalPlan()
        {
            plannedLandingTarget = null;
            plannedLandingCollider = null;
            plannedMovingPlatform = null;
            plannedHoldDuration = 0f;
            plannedFlightTime = 0f;
            heldAbility = null;
            holdUntil = 0d;
        }

        void HandleAirborneBounce(CharacterAbilityController abilities)
        {
            // A bounce is already a new ballistic launch. Never request another jump
            // from it; replace the enemy landing plan with a safe ground landing.
            Collider excludedContact = plannedLandingCollider;
            ClearTraversalPlan();
            suppressJumpRequestsUntil = Time.timeAsDouble + .18d;
            float direction = currentTarget == null ? Mathf.Sign(abilities.Motor.Result.Velocity.x) :
                Mathf.Sign(currentTarget.position.x - transform.position.x);
            if (Mathf.Approximately(direction, 0f)) direction = 1f;
            TryPlanCurrentAirborneLanding(direction, abilities.Motor.Result.Velocity, excludedContact);
        }

        bool TryPlanCurrentAirborneLanding(float direction, Vector3 velocity, Collider excludedContact)
        {
            CharacterAITraversalSettings settings = definition.Traversal;
            RaycastHit best = default;
            float bestScore = float.MaxValue;
            float horizontalSpeed = Mathf.Max(1.5f, Mathf.Abs(velocity.x));
            for (float distance = settings.EdgeProbeDistance; distance <= settings.MaximumJumpReach * 1.6f;
                 distance += .2f)
            {
                Vector3 origin = transform.position + Vector3.right * direction * distance +
                                 Vector3.up * 1.2f;
                if (!RaycastGround(origin, settings.LandingProbeDepth + 2f, out RaycastHit hit)) continue;
                if (hit.collider == excludedContact ||
                    (excludedContact != null && hit.transform.IsChildOf(excludedContact.transform))) continue;
                float time = distance / horizontalSpeed;
                float predictedY = transform.position.y + velocity.y * time - 9.25f * time * time;
                float score = Mathf.Abs(predictedY - hit.point.y);
                if (score >= bestScore) continue;
                best = hit;
                bestScore = score;
            }
            if (best.collider == null) return false;
            ICharacterMovingPlatform moving = best.collider.GetComponentInParent(
                typeof(ICharacterMovingPlatform)) as ICharacterMovingPlatform;
            SetTraversalPlan(null, best.collider, moving, best.point, 0f,
                Mathf.Abs(best.point.x - transform.position.x) / horizontalSpeed);
            return true;
        }

        static bool IsLastAcceptedBounce(CharacterAbilityController abilities)
        {
            if (abilities.LastRequestResult != AbilityRequestResult.Accepted ||
                abilities.LastRequestedAbility is not SequenceAbilityDefinition sequence) return false;
            GameSystems.Sequencing.GameAction[] actions = sequence.Sequence.Actions;
            for (int i = 0; i < actions.Length; i++)
                if (actions[i] is BounceAction) return true;
            return false;
        }

        bool RaycastGround(Vector3 origin, float distance, out RaycastHit valid)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance,
                definition.LineOfSightMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform == transform || hits[i].transform.IsChildOf(transform)) continue;
                valid = hits[i];
                return true;
            }
            valid = default;
            return false;
        }

        bool IsGrounded()
        {
            CharacterAbilityController abilities = GetComponent<CharacterAbilityController>();
            return abilities?.Motor != null && abilities.Motor.Result.Ground.IsGrounded;
        }

        bool TryGetAirborneCharacterTarget(out Transform result)
        {
            result = null;
            if (definition == null) return false;
            float bestScore = float.MaxValue;
            float lookAhead = definition.Traversal.CharacterLookAhead;
            foreach (CharacterAbilityController candidate in CharacterAbilityRegistry.Controllers)
            {
                if (candidate == null || candidate.gameObject == gameObject) continue;
                if (!HasAbility(candidate, definition.Traversal.AirborneTargetAbility)) continue;
                Vector3 delta = candidate.transform.position - transform.position;
                if (delta.y > .35f || delta.y < -3.5f || Mathf.Abs(delta.x) > lookAhead) continue;
                float score = Mathf.Abs(delta.x) + Mathf.Abs(delta.y) * .18f;
                if (score >= bestScore) continue;
                bestScore = score;
                result = candidate.transform;
            }
            return result != null;
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
    }
}
