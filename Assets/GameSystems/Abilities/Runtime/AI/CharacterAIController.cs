using UnityEngine;
using GameSystems.Hooks;

namespace GameSystems.Abilities
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-400)]
    public sealed class CharacterAIController : MonoBehaviour, ICharacterCommandSource
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

        public void Configure(CharacterAIDefinition value) => definition = value;
        public void SetTarget(Transform value) => targetOverride = value;

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
            foreach (CharacterAbilityController candidate in CharacterRegistry.Controllers)
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
    }
}
