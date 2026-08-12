using GameSystems.Actions;

namespace GameSystems.Abilities
{
    public sealed class SequenceAbilityRuntime : AbilityRuntime
    {
        readonly GameActionRunner runner = new();
        bool completionRequested;
        SequenceAbilityDefinition Data => (SequenceAbilityDefinition)Definition;

        public override bool CanStart(in AbilityRequest request)
        {
            AbilityEvaluationContext evaluation = new(
                Context, this, request, Context.Motor != null ? Context.Motor.Result : default);
            GameActionContext actionContext = new(Context.Owner, evaluation, Context, this);
            return Data.Sequence.CanRun(actionContext);
        }

        internal override bool Refresh(in AbilityRequest request)
        {
            if (!Data.RefreshWhileActive) return false;
            UpdateLastRequest(request);
            OnStart(request);
            return true;
        }

        protected override void OnStart(in AbilityRequest request)
        {
            completionRequested = false;
            AbilityEvaluationContext evaluation = new(
                Context, this, request, Context.Motor != null ? Context.Motor.Result : default);
            GameActionContext actionContext = new(Context.Owner, evaluation, Context, this);
            runner.Initialize(Data.Sequence.Actions, actionContext);
            runner.Start();
            CompleteIfFinished();
        }

        protected override void OnBeforeMotor(float deltaTime)
        {
            if (!runner.IsRunning) { CompleteIfFinished(); return; }
            runner.Tick(deltaTime);
            CompleteIfFinished();
        }

        protected override void OnAfterMotor(in CharacterMotorResult result)
        {
            if (!runner.IsRunning) { CompleteIfFinished(); return; }
            runner.TickLate();
            CompleteIfFinished();
        }

        protected override void OnStop(AbilityStopReason reason) => runner.Stop();

        void CompleteIfFinished()
        {
            if (runner.IsRunning || completionRequested || !Data.CompleteWhenSequenceEnds) return;
            completionRequested = true;
            Complete();
        }
    }
}
