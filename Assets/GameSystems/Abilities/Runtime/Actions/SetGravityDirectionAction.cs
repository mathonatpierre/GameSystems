using System;
using GameSystems.Characters;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class SetGravityDirectionAction : GameAction
    {
        [SerializeField, Tooltip("World-space direction in which gravity pulls.")]
        Vector3 direction = Vector3.down;

        public override string Summary => $"Set gravity direction = {direction}";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : GameActionRuntime
        {
            protected override bool Tick(float deltaTime)
            {
                ICharacterMotor motor = Context.Get<CharacterRuntimeContext>().Motor;
                if (motor == null) { Fail("Missing character motor."); return true; }
                Vector3 direction = ((SetGravityDirectionAction)Definition).direction;
                if (direction.sqrMagnitude < .0001f)
                { Fail("Gravity direction cannot be zero."); return true; }
                CharacterMotorCommands commands = motor.Commands;
                commands.HasGravityDirection = true;
                commands.GravityDirection = direction.normalized;
                motor.Commands = commands;
                return false;
            }
        }
    }
}
