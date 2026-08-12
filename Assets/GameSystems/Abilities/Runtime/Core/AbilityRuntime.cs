using UnityEngine;

namespace GameSystems.Abilities
{
    public abstract class AbilityRuntime
    {
        public AbilityDefinition Definition { get; private set; }
        public CharacterRuntimeContext Context { get; private set; }
        public AbilityPhase Phase { get; private set; } = AbilityPhase.Inactive;
        public float ActiveTime { get; private set; }
        public Vector3 StartPosition { get; private set; }
        public AbilityRequest LastRequest { get; private set; }
        public bool HasPendingTransition { get; internal set; }

        public bool IsActive => Phase != AbilityPhase.Inactive;

        internal void Initialize(AbilityDefinition definition, CharacterRuntimeContext context)
        {
            Definition = definition;
            Context = context;
            OnInitialize();
        }

        public virtual bool CanStart(in AbilityRequest request) => true;

        // Event-driven abilities such as a bounce may be requested more than once
        // before the next motor tick. They can absorb the newer request instead of
        // silently losing the gameplay impulse as "AlreadyActive".
        internal virtual bool Refresh(in AbilityRequest request) => false;

        protected void UpdateLastRequest(in AbilityRequest request) => LastRequest = request;

        internal void Start(in AbilityRequest request)
        {
            ActiveTime = 0f;
            StartPosition = Context.Transform.position;
            LastRequest = request;
            HasPendingTransition = false;
            Phase = AbilityPhase.Starting;
            OnStart(request);
            if (Phase == AbilityPhase.Starting) Phase = AbilityPhase.Active;
        }

        internal void TickBeforeMotor(float deltaTime)
        {
            ActiveTime += deltaTime;
            OnBeforeMotor(deltaTime);
        }

        internal void TickAfterMotor(in CharacterMotorResult result)
        {
            OnAfterMotor(result);
            if (IsActive) Context.Abilities.EvaluateTransitions(this, AbilityTransitionTrigger.WhileActive, result);
        }

        public void Complete() => Context.Abilities.RequestStop(this, AbilityStopReason.Completed);
        public void Cancel() => Context.Abilities.RequestStop(this, AbilityStopReason.Cancelled);

        internal void Stop(AbilityStopReason reason)
        {
            if (!IsActive) return;
            Phase = AbilityPhase.Completing;
            OnStop(reason);
            Phase = AbilityPhase.Inactive;
            HasPendingTransition = false;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnStart(in AbilityRequest request) { }
        protected virtual void OnBeforeMotor(float deltaTime) { }
        protected virtual void OnAfterMotor(in CharacterMotorResult result) { }
        protected virtual void OnStop(AbilityStopReason reason) { }
    }
}
