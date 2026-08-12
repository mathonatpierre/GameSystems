using System;
using System.Collections.Generic;
using GameSystems.Feedbacks;
using GameSystems.Actions;
using UnityEngine;

namespace GameSystems.Abilities
{
    [DefaultExecutionOrder(-300)]
    public sealed class CharacterAbilityController : MonoBehaviour, IAbilityLockService
    {
        [SerializeField] AbilitySet abilitySet;

        readonly Dictionary<AbilityDefinition, AbilityRuntime> runtimes = new();
        readonly List<AbilityRuntime> orderedRuntimes = new();
        readonly List<AbilityRuntime> active = new(8);
        readonly List<(AbilityRuntime runtime, AbilityStopReason reason)> pendingStops = new(4);
        readonly List<AbilityRequest> pendingRequests = new(4);
        readonly Dictionary<AbilityDefinition, double> lastStoppedAt = new();
        readonly List<GameActionRunner> transitionActionRunners = new(4);
        readonly CharacterRequestBuffer requests = new();
        readonly List<AbilityRequest> orderedRequests = new(16);
        CharacterRuntimeContext context;
        ICharacterMotor motor;
        bool locked;
        bool simulateMotorWhileLocked;
        AbilityAuthority exclusiveAuthorities;

        public CharacterRuntimeContext Context => context;
        public IReadOnlyList<AbilityRuntime> ActiveAbilities => active;
        public AbilityAuthority ExclusiveAuthorities => exclusiveAuthorities;
        public AbilitySet AbilitySet => abilitySet;
        public ICharacterMotor Motor => motor;
        public bool IsAbilityLocked => locked;
        public bool SimulateMotorWhileLocked => simulateMotorWhileLocked;
        public AbilityDefinition LastRequestedAbility { get; private set; }
        public AbilityRequestResult LastRequestResult { get; private set; }
        public string LastTransitionLabel { get; private set; }
        public AbilityDefinition LastTransitionSource { get; private set; }
        public AbilityDefinition LastTransitionTarget { get; private set; }
        public void Configure(AbilitySet value)
        {
            abilitySet = value;
            if (context != null) BuildAbilitySet();
        }

        void Awake()
        {
            context = new CharacterRuntimeContext(gameObject) { Abilities = this };
            motor = GetComponent(typeof(ICharacterMotor)) as ICharacterMotor;
            context.Motor = motor;
            context.Bind<IAbilityLockService>(this);
            context.Bind(GetComponent<GameSystems.Stats.CharacterStats>());
            BuildAbilitySet();
            StartAutomaticAbilities();
        }

        void OnEnable() => CharacterRegistry.Register(this);

        void Update()
        {
            orderedRequests.Clear();
            for (int i = 0; i < requests.Requests.Count; i++) orderedRequests.Add(requests.Requests[i]);
            requests.Clear();
            orderedRequests.Sort((a, b) => b.Ability.Priority.CompareTo(a.Ability.Priority));
            if (!locked)
                for (int i = 0; i < orderedRequests.Count; i++) Request(orderedRequests[i]);
            TickBeforeMotor(Time.deltaTime);
        }

        public void Submit(in AbilityRequest request) => requests.Add(request);
        public void OnMotorStepped(in CharacterMotorResult result) => TickAfterMotor(result);

        public void BeginAbilityLock(bool keepSimulatingMotor)
        { locked = true; simulateMotorWhileLocked = keepSimulatingMotor; }

        public void EndAbilityLock()
        { simulateMotorWhileLocked = false; locked = false; }

        void TickBeforeMotor(float deltaTime)
        {
            for (int i = transitionActionRunners.Count - 1; i >= 0; i--)
                if (transitionActionRunners[i].Tick(deltaTime)) transitionActionRunners.RemoveAt(i);
            // Automatic abilities are state evaluators. Start them from the stable
            // motor result of the previous frame, before iterating active runtimes.
            // Starting them from TickAfterMotor mutated the active list mid-pass and
            // could leave Fall permanently inactive.
            StartAutomaticAbilities();
            for (int i = 0; i < active.Count; i++) active[i].TickBeforeMotor(deltaTime);
            FlushStops();
        }

        void TickAfterMotor(in CharacterMotorResult result)
        {
            for (int i = 0; i < active.Count; i++) active[i].TickAfterMotor(result);
            FlushStops();
        }

        void BuildAbilitySet()
        {
            ResetAll();
            runtimes.Clear();
            orderedRuntimes.Clear();
            AbilitySet selectedSet = abilitySet;
            if (selectedSet != null)
            foreach (AbilityDefinition definition in selectedSet.Abilities)
            {
                EnsureRuntime(definition);
            }
            orderedRuntimes.Sort((a, b) => b.Definition.Priority != a.Definition.Priority
                ? b.Definition.Priority.CompareTo(a.Definition.Priority)
                : IndexInSet(a.Definition).CompareTo(IndexInSet(b.Definition)));
        }

        int IndexInSet(AbilityDefinition ability)
        {
            IReadOnlyList<AbilityDefinition> definitions = abilitySet?.Abilities;
            if (definitions == null) return int.MaxValue;
            for (int i = 0; i < definitions.Count; i++) if (definitions[i] == ability) return i;
            return int.MaxValue;
        }

        void EnsureRuntime(AbilityDefinition definition)
        {
            if (definition == null || runtimes.ContainsKey(definition)) return;
            AbilityRuntime runtime = definition.CreateRuntime();
            if (runtime == null) throw new InvalidOperationException($"{definition.name} returned no runtime");
            runtime.Initialize(definition, context);
            runtimes.Add(definition, runtime);
            orderedRuntimes.Add(runtime);
        }

        void SortRuntimes()
        {
            orderedRuntimes.Sort((a, b) => b.Definition.Priority != a.Definition.Priority
                ? b.Definition.Priority.CompareTo(a.Definition.Priority)
                : IndexInSet(a.Definition).CompareTo(IndexInSet(b.Definition)));
        }

        public bool Request(in AbilityRequest request) => RequestDetailed(request) == AbilityRequestResult.Accepted;

        public void Cancel(AbilityDefinition ability)
        {
            if (ability != null && runtimes.TryGetValue(ability, out AbilityRuntime runtime) && runtime.IsActive)
                RequestStop(runtime, AbilityStopReason.Cancelled);
        }

        AbilityRequestResult RequestDetailed(in AbilityRequest request)
        {
            LastRequestedAbility = request.Ability;
            if (request.Ability == null) return Record(request, AbilityRequestResult.MissingAbility);
            if (!runtimes.TryGetValue(request.Ability, out AbilityRuntime candidate)) return Record(request, AbilityRequestResult.NotInAbilitySet);
            if (candidate.IsActive)
                return Record(request, candidate.Refresh(request)
                    ? AbilityRequestResult.Accepted
                    : AbilityRequestResult.AlreadyActive);
            if (GetCooldownRemaining(request.Ability) > 0f) return Record(request, AbilityRequestResult.OnCooldown);
            if (!candidate.CanStart(request)) return Record(request, AbilityRequestResult.RejectedByRuntime);

            for (int i = active.Count - 1; i >= 0; i--)
            {
                AbilityRuntime other = active[i];
                bool conflicts =
                    (other.Definition.ExclusiveAuthority & request.Ability.RequiredAuthority) != 0 ||
                    (request.Ability.ExclusiveAuthority & other.Definition.RequiredAuthority) != 0;
                if (!conflicts) continue;
                if (!other.Definition.CanBeInterruptedBy(request.Ability))
                    return Record(request, other.Definition.InterruptionPolicy == AbilityInterruptionPolicy.HigherOrEqualPriority
                        ? AbilityRequestResult.LowerAuthorityPriority
                        : AbilityRequestResult.InterruptionBlocked);
                StopNow(other, AbilityStopReason.Interrupted);
            }

            candidate.Start(request);
            active.Add(candidate);
            exclusiveAuthorities |= request.Ability.ExclusiveAuthority;
            PlayFeedback(request.Ability.StartFeedback, request.Value);
            return Record(request, AbilityRequestResult.Accepted);
        }

        public float GetCooldownRemaining(AbilityDefinition ability)
        {
            if (ability == null || ability.Cooldown <= 0f || !lastStoppedAt.TryGetValue(ability, out double stopped))
                return 0f;
            return Mathf.Max(0f, ability.Cooldown - (float)(Time.timeAsDouble - stopped));
        }

        AbilityRequestResult Record(in AbilityRequest request, AbilityRequestResult result)
        {
            LastRequestResult = result;
            if (request.Source is IAbilityRequestObserver observer)
                observer.OnAbilityRequestResolved(request, result);
            return result;
        }

        public bool Request(AbilityDefinition ability, UnityEngine.Object source = null, float value = 1f)
            => Request(new AbilityRequest(ability, source != null ? source : this, value, Time.timeAsDouble));

        public bool RequestReaction(ReactionId reactionId, float value = 1f, UnityEngine.Object source = null)
        {
            return RequestReaction(FindReaction(reactionId), value, source);
        }

        public bool RequestReaction(string customReactionId, float value = 1f, UnityEngine.Object source = null)
        {
            return RequestReaction(FindReaction(customReactionId), value, source);
        }

        public bool RequestReaction(ReactionDefinition reaction, float value = 1f, UnityEngine.Object source = null)
        {
            if (reaction == null) return false;
            bool existed = runtimes.ContainsKey(reaction);
            EnsureRuntime(reaction);
            if (!existed) SortRuntimes();
            return Request(reaction, source, value);
        }

        ReactionDefinition FindReaction(ReactionId reactionId)
        {
            for (int i = 0; i < orderedRuntimes.Count; i++)
                if (orderedRuntimes[i].Definition is ReactionDefinition reaction && reaction.Matches(reactionId))
                    return reaction;
            return null;
        }

        ReactionDefinition FindReaction(string customReactionId)
        {
            for (int i = 0; i < orderedRuntimes.Count; i++)
                if (orderedRuntimes[i].Definition is ReactionDefinition reaction && reaction.Matches(customReactionId))
                    return reaction;
            return null;
        }

        void StartAutomaticAbilities()
        {
            // A reaction such as Respawn or Death may keep simulating the motor while
            // presentation feedback is playing. Do not let automatic state abilities
            // restart from that transient motor state (Fall would otherwise apply its
            // lethal transition once per frame until the checkpoint teleport).
            if (locked) return;

            for (int i = 0; i < orderedRuntimes.Count; i++)
            {
                AbilityDefinition definition = orderedRuntimes[i].Definition;
                if (definition.AutoStart) Request(definition, this);
            }
        }

        internal void EvaluateTransitions(AbilityRuntime runtime, AbilityTransitionTrigger trigger,
            in CharacterMotorResult motor, bool mayCompleteSource = true)
        {
            if (runtime == null || runtime.HasPendingTransition) return;
            AbilityTransitionDefinition winner = null;
            AbilityEvaluationContext evaluation = new(context, runtime, runtime.LastRequest, motor);
            AbilityTransitionDefinition[] transitions = runtime.Definition.Transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AbilityTransitionDefinition transition = transitions[i];
                if (transition == null || transition.Trigger != trigger || !transition.Evaluate(evaluation)) continue;
                if (winner == null || transition.Priority > winner.Priority) winner = transition;
            }
            if (winner == null) return;
            LastTransitionLabel = winner.Label;
            LastTransitionSource = runtime.Definition;
            LastTransitionTarget = winner.Target;
            runtime.HasPendingTransition = true;
            GameActionRunner transitionActions = winner.ExecuteActions(evaluation);
            if (transitionActions.IsRunning) transitionActionRunners.Add(transitionActions);
            if (mayCompleteSource && winner.CompleteSource)
                RequestStop(runtime, AbilityStopReason.Completed);
            if (winner.Target != null)
                pendingRequests.Add(new AbilityRequest(winner.Target, this, runtime.LastRequest.Value, Time.timeAsDouble));
        }

        internal void RequestStop(AbilityRuntime runtime, AbilityStopReason reason)
        {
            if (runtime != null && runtime.IsActive) pendingStops.Add((runtime, reason));
        }

        void FlushStops()
        {
            for (int i = 0; i < pendingStops.Count; i++) StopNow(pendingStops[i].runtime, pendingStops[i].reason);
            pendingStops.Clear();
            if (pendingRequests.Count == 0) return;
            int queuedCount = pendingRequests.Count;
            for (int i = 0; i < queuedCount; i++) Request(pendingRequests[i]);
            pendingRequests.RemoveRange(0, queuedCount);
        }

        void StopNow(AbilityRuntime runtime, AbilityStopReason reason)
        {
            if (runtime == null || !runtime.IsActive) return;
            AbilityTransitionTrigger? trigger = reason switch
            {
                AbilityStopReason.Completed => AbilityTransitionTrigger.OnCompleted,
                AbilityStopReason.Cancelled => AbilityTransitionTrigger.OnCancelled,
                AbilityStopReason.Interrupted => AbilityTransitionTrigger.OnInterrupted,
                _ => null
            };
            if (trigger.HasValue && !runtime.HasPendingTransition)
                EvaluateTransitions(runtime, trigger.Value,
                    context.Motor != null ? context.Motor.Result : default,
                    mayCompleteSource: false);
            runtime.Stop(reason);
            lastStoppedAt[runtime.Definition] = Time.timeAsDouble;
            active.Remove(runtime);
            RecalculateAuthorities();
            if (reason == AbilityStopReason.Completed)
                PlayFeedback(runtime.Definition.CompleteFeedback, 1f);
        }

        public int CountActive(AbilityCategory category)
        {
            int count = 0;
            for (int i = 0; i < active.Count; i++)
                if (active[i].Definition.Category == category) count++;
            return count;
        }

        void PlayFeedback(FeedbackSequence sequence, float intensity)
        {
            if (sequence == null) return;
            Animator animator = GetComponentInChildren<Animator>();
            Renderer renderer = GetComponentInChildren<Renderer>();
            GameObject visuals = animator != null ? animator.gameObject
                : renderer != null ? renderer.gameObject : gameObject;
            FeedbackContext feedback = FeedbackContext.From(gameObject)
                .WithIntensity(Mathf.Max(.01f, intensity))
                .Bind("Character", gameObject)
                .Bind("Visual", visuals)
                .Bind("Visuals", visuals)
                .Bind("Slime", visuals);
            FeedbackService.Play(sequence, feedback);
        }

        void RecalculateAuthorities()
        {
            exclusiveAuthorities = AbilityAuthority.None;
            for (int i = 0; i < active.Count; i++) exclusiveAuthorities |= active[i].Definition.ExclusiveAuthority;
        }

        public void ResetAll()
        {
            for (int i = active.Count - 1; i >= 0; i--) active[i].Stop(AbilityStopReason.Reset);
            active.Clear();
            pendingStops.Clear();
            pendingRequests.Clear();
            lastStoppedAt.Clear();
            for (int i = 0; i < transitionActionRunners.Count; i++) transitionActionRunners[i].Stop();
            transitionActionRunners.Clear();
            exclusiveAuthorities = AbilityAuthority.None;
        }

        internal void ResetForRespawn(AbilityRuntime preservedRuntime)
        {
            // A teleport is a discontinuity, not a landing. Airborne actions such
            // as Jump, Bounce, Fall and AirLocomotion must not survive it waiting
            // for a JustLanded event that is intentionally not emitted.
            pendingStops.Clear();
            pendingRequests.Clear();
            for (int i = active.Count - 1; i >= 0; i--)
            {
                AbilityRuntime runtime = active[i];
                if (runtime == preservedRuntime) continue;
                runtime.Stop(AbilityStopReason.Reset);
                active.RemoveAt(i);
            }
            RecalculateAuthorities();
        }

        void OnDisable()
        {
            CharacterRegistry.Unregister(this);
            ResetAll();
        }
    }
}
