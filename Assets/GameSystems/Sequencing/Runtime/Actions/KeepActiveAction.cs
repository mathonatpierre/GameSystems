using System;

namespace GameSystems.Sequencing
{
    [Serializable]
    public sealed class KeepActiveAction : GameAction
    {
        public override string Summary => "Keep sequence active";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            protected internal override bool Tick(float deltaTime) => false;
        }
    }
}
