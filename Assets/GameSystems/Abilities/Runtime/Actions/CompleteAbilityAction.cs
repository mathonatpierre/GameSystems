using System;
using GameSystems.Abilities;
using GameSystems.Sequencing;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class CompleteAbilityAction : GameAction
    {
        public override string Summary => "Complete ability";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                Context.Get<AbilityRuntime>().Complete();
            }
        }
    }
}
