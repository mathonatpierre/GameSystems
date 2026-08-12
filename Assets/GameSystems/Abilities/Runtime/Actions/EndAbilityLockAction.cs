using System;
using GameSystems.Abilities;
using GameSystems.Sequencing;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class EndAbilityLockAction : GameAction
    {
        public override string Summary => "End ability lock";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                Context.Get<CharacterRuntimeContext>().Resolve<IAbilityLockService>()?.EndAbilityLock();
            }
        }
    }
}
