namespace GameSystems.Sequencing
{
    public abstract class InstantActionRuntime : GameActionRuntime
    {
        bool executed;
        protected internal override void OnEnter() { base.OnEnter(); executed = false; }
        protected internal override bool Tick(float deltaTime) { if (!executed) { executed = true; Execute(); } return true; }
        protected abstract void Execute();
    }
}
