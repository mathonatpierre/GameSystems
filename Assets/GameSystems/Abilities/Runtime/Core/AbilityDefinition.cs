using GameSystems.Feedbacks;
using UnityEngine;
using UnityEngine.Serialization;
using System;

namespace GameSystems.Abilities
{
    public abstract class AbilityDefinition : ScriptableObject
    {
        [Header("Scheduling")]
        [FormerlySerializedAs("autoStart")]
        [SerializeField] AbilityActivationPolicy activationPolicy;
        [SerializeField] int priority;
        [SerializeField, Min(0f)] float cooldown;
        [SerializeField] AbilityAuthority requiredAuthority;
        [SerializeField] AbilityAuthority exclusiveAuthority;

        [Header("Interruptions")]
        [FormerlySerializedAs("canBeInterrupted")]
        [SerializeField] AbilityInterruptionPolicy interruptionPolicy = AbilityInterruptionPolicy.HigherOrEqualPriority;

        [Header("Transitions")]
        [SerializeField] AbilityTransitionDefinition[] transitions;

        [Header("Presentation")]
        [SerializeField] AbilityAnimationIntent animationIntent;
        [SerializeField] FeedbackSequence startFeedback;
        [SerializeField] FeedbackSequence completeFeedback;

        public AbilityActivationPolicy ActivationPolicy => activationPolicy;
        public bool AutoStart => activationPolicy is AbilityActivationPolicy.Automatic or AbilityActivationPolicy.Persistent;
        public int Priority => priority;
        public float Cooldown => cooldown;
        public AbilityAuthority RequiredAuthority => requiredAuthority;
        public AbilityAuthority ExclusiveAuthority => exclusiveAuthority;
        public FeedbackSequence StartFeedback => startFeedback;
        public FeedbackSequence CompleteFeedback => completeFeedback;
        public AbilityAnimationIntent AnimationIntent => animationIntent;
        public AbilityTransitionDefinition[] Transitions => transitions ?? Array.Empty<AbilityTransitionDefinition>();
        public AbilityInterruptionPolicy InterruptionPolicy => interruptionPolicy;
        public virtual AbilityCategory Category => AbilityCategory.Ability;

        public bool CanBeInterruptedBy(AbilityDefinition incoming)
        {
            if (incoming == null) return false;
            return interruptionPolicy switch
            {
                AbilityInterruptionPolicy.Never => false,
                AbilityInterruptionPolicy.HigherOrEqualPriority => incoming.Priority >= Priority,
                AbilityInterruptionPolicy.AnyConflicting => true,
                _ => false
            };
        }

        public abstract AbilityRuntime CreateRuntime();
    }
}
