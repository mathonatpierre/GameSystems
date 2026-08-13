using UnityEngine;
using GameSystems.Hooks;
using GameSystems.Abilities;
using UnityEngine.Scripting.APIUpdating;
using System.Collections.Generic;

namespace GameSystems.Characters
{
    [MovedFrom(true, "GameSystems.Abilities", "GameSystems.Abilities", "CharacterAIController")]
    [DisallowMultipleComponent, DefaultExecutionOrder(-400)]
    public sealed class CharacterAIController : MonoBehaviour, ICharacterCommandSource, IHorizontalInputProvider
    {
        [SerializeField] CharacterAIDefinition definition;
        [SerializeField, Tooltip("Optional fixed target. Without one, the nearest other ability controller is selected.")]
        Transform targetOverride;

        Transform currentTarget;
        double nextDecisionAt;
        readonly CharacterRequestBuffer pending = new();

        public CharacterAIDefinition Definition => definition;
        public Transform CurrentTarget => currentTarget;
        public CharacterAIDecision CurrentDecision { get; private set; }
        public CharacterAIContext LastContext { get; private set; }
        public float Horizontal
        {
            get
            {
                if (currentTarget == null) return 0f;
                if (!IsGrounded() && TryGetAirborneCharacterTarget(out Transform airborneTarget))
                {
                    float correction = airborneTarget.position.x - transform.position.x;
                    return Mathf.Abs(correction) < .08f ? 0f : Mathf.Clamp(correction * 1.8f, -1f, 1f);
                }
                float direction = Mathf.Sign(currentTarget.position.x - transform.position.x);
                return ShouldWaitAtEdge(direction) ? 0f : direction;
            }
        }

        public void Configure(CharacterAIDefinition value) => definition = value;
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

            CharacterAIDecision winner = null;
            CharacterAIDecision[] decisions = definition.Decisions;
            for (int i = 0; i < decisions.Length; i++)
            {
                CharacterAIDecision decision = decisions[i];
                if (decision == null) continue;
                decision.ClearDebug();
                if (!decision.Evaluate(LastContext, Time.timeAsDouble)) continue;
                if (winner == null || decision.Priority > winner.Priority) winner = decision;
            }
            if (CurrentDecision != null && CurrentDecision != winner)
                context.Abilities.Cancel(CurrentDecision.Ability);
            CurrentDecision = winner;
            if (winner == null) return;
            winner.MarkSelected(Time.timeAsDouble);
            requests.Add(new AbilityRequest(winner.Ability, this, 1f, Time.timeAsDouble));
        }

        void Update()
        {
            CharacterAbilityController abilities = GetComponent<CharacterAbilityController>();
            if (abilities == null) return;
            pending.Clear();
            CollectCommands(abilities.Context, pending);
            for (int i = 0; i < pending.Requests.Count; i++) abilities.Submit(pending.Requests[i]);
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
            for (float distance = settings.EdgeProbeDistance; distance <= settings.MaximumJumpReach;
                 distance += .22f)
            {
                Vector3 origin = transform.position + Vector3.right * direction * distance + Vector3.up * 1.1f;
                if (RaycastGround(origin, settings.LandingProbeDepth, out landing)) return true;
            }
            landing = default;
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
