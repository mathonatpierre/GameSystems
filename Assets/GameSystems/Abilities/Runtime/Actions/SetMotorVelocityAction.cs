using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class SetMotorVelocityAction : GameAction
    {
        [SerializeField, Tooltip("World-space velocity assigned to the character motor.")] Vector3 velocity;
        public override string Summary => $"Set motor velocity = {velocity}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                if (Context.Get<CharacterRuntimeContext>().Motor is not ICharacterMotorControl motor) { Fail("Motor cannot be controlled."); return; }
                motor.SetVelocity(((SetMotorVelocityAction)Definition).velocity);
            }
        }
    }
}
