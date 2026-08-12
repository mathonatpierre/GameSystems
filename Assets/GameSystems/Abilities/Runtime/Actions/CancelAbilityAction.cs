using System;
using GameSystems.Actions;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class CancelAbilityAction : GameAction
    {
        public override string Summary => "Cancel current ability";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute() => Context.Get<AbilityRuntime>().Cancel();
        }
    }
}
