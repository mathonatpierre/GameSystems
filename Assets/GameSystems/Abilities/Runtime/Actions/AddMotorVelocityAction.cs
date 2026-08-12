using System;
using GameSystems.Actions;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class AddMotorVelocityAction : GameAction
    {
        [SerializeField, Tooltip("World-space velocity added to the current motor velocity.")] Vector3 velocity;
        public override string Summary => $"Add motor velocity {velocity}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                if (Context.Get<CharacterRuntimeContext>().Motor is not ICharacterMotorControl motor) { Fail("Motor cannot be controlled."); return; }
                motor.SetVelocity(motor.Velocity + ((AddMotorVelocityAction)Definition).velocity);
            }
        }
    }
}
