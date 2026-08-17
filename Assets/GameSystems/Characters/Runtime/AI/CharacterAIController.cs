using UnityEngine;
using GameSystems.Hooks;
using GameSystems.Abilities;
using UnityEngine.Scripting.APIUpdating;
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
        double holdUntil;
        AbilityDefinition heldAbility;
        readonly CharacterAIBlackboard blackboard = new();
        CharacterAISensors sensors;
        CharacterBehaviorTreeRuntime behaviorRuntime;

        CharacterAIBlackboard.TraversalState Traversal => blackboard.Traversal;

        public CharacterAIDefinition Definition => definition;
        public Transform CurrentTarget => currentTarget;
        public CharacterAIBlackboard Blackboard => blackboard;
        public string CurrentBehavior => behaviorRuntime?.ActiveNodeId is string id
            ? definition?.BehaviorTree?.Find(id)?.Title : null;
        public CharacterAIContext LastContext { get; private set; }
        public CharacterBehaviorTreeRuntime BehaviorRuntime => behaviorRuntime;
        public float Horizontal => blackboard.HorizontalIntent;

        public void Configure(CharacterAIDefinition value)
        {
            definition = value;
            behaviorRuntime = definition?.BehaviorTree?.CreateRuntime();
        }
        public void SetTarget(Transform value) => targetOverride = value;

        public void CollectCommands(CharacterRuntimeContext context, CharacterRequestBuffer requests)
        {
            if (definition == null || Time.timeAsDouble < nextDecisionAt) return;
            nextDecisionAt = Time.timeAsDouble + definition.DecisionInterval;
            sensors ??= new CharacterAISensors(this);
            currentTarget = sensors.ResolveTarget(context, definition, targetOverride);
            float wallProbeDistance = Mathf.Max(1.05f, definition.Traversal.MaximumJumpReach);
            float traversalDirection = definition.TraversalDirection;
            if (Mathf.Approximately(traversalDirection, 0f))
                traversalDirection = currentTarget != null
                    ? Mathf.Sign(currentTarget.position.x - transform.position.x) : 1f;
            bool nearbyWall = sensors.TryFindNearbyWall(wallProbeDistance, .55f,
                definition.LineOfSightMask, traversalDirection, out float wallDirection);
            blackboard.Update(context, currentTarget,
                sensors.HasLineOfSight(context, currentTarget, definition.LineOfSightMask),
                nearbyWall, wallDirection);
            LastContext = new CharacterAIContext(context, this, blackboard);

            behaviorRuntime ??= definition.BehaviorTree?.CreateRuntime();
            if (behaviorRuntime == null) return;
            behaviorRuntime.Tick(LastContext, Time.timeAsDouble);
            AbilityDefinition requested = behaviorRuntime.RequestedAbility;
            if (requested == null) return;
            requests.Add(new AbilityRequest(requested, this, 1f, Time.timeAsDouble));
        }

        void Update()
        {
            CharacterAbilityController abilities = GetComponent<CharacterAbilityController>();
            if (abilities == null) return;
            pending.Clear();
            CollectCommands(abilities.Context, pending);
            for (int i = 0; i < pending.Requests.Count; i++) abilities.Submit(pending.Requests[i]);
        }

        public bool AnyAbilityHeld => heldAbility != null && Time.timeAsDouble < holdUntil;

        public bool IsHeld(AbilityDefinition ability) =>
            ability != null && ability == heldAbility && Time.timeAsDouble < holdUntil;

        public void OnAbilityRequestResolved(in AbilityRequest request, AbilityRequestResult result)
        {
            CharacterAbilityController abilities = GetComponent<CharacterAbilityController>();
            if (result != AbilityRequestResult.Accepted || Traversal.HoldDuration <= 0f ||
                Time.timeAsDouble - Traversal.CreatedAt > .3d ||
                abilities?.Motor?.Result.Ground.IsGrounded != true) return;
            heldAbility = request.Ability;
            holdUntil = Time.timeAsDouble + Traversal.HoldDuration;
        }
    }
}
