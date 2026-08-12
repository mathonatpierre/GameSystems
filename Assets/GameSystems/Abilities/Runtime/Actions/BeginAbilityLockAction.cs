using System;
using GameSystems.Abilities;
using GameSystems.Actions;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class BeginAbilityLockAction : GameAction
    {
        [SerializeField, Tooltip("Continue motor simulation while character input and abilities are locked.")]
        bool keepSimulatingMotor;

        public override string Summary => $"Begin ability lock, simulate motor = {keepSimulatingMotor.ToString().ToLowerInvariant()}";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                BeginAbilityLockAction data = (BeginAbilityLockAction)Definition;
                Context.Get<CharacterRuntimeContext>().Resolve<IAbilityLockService>()?.BeginAbilityLock(data.keepSimulatingMotor);
            }
        }
    }
}
