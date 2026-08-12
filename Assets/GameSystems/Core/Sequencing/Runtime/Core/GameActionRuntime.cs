namespace GameSystems.Actions
{
    public abstract class GameActionRuntime
    {
        public GameAction Definition { get; private set; }
        protected GameActionContext Context { get; private set; }
        public bool Failed { get; private set; }
        public string FailureReason { get; private set; }
        internal void Initialize(GameAction definition, in GameActionContext context) { Definition = definition; Context = context; }
        protected internal virtual void OnEnter() { Failed = false; FailureReason = null; }
        protected internal abstract bool Tick(float deltaTime);
        protected internal virtual bool TickLate() => false;
        protected internal virtual void OnExit() { }
        protected void Fail(string reason = null) { Failed = true; FailureReason = reason; }
    }
}
