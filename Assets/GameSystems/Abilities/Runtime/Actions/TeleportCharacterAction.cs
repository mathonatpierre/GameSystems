using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class TeleportCharacterAction : GameAction
    {
        [SerializeField, Tooltip("World position or offset used for teleportation.")] Vector3 position;
        [SerializeField, Tooltip("Treat Position as an offset from the current character position.")] bool relative;
        [SerializeField, Tooltip("Reset motor velocity and contacts before teleporting.")] bool resetMotor = true;
        public override string Summary => $"Teleport character {(relative ? "by" : "to")} {position}, reset motor = {resetMotor.ToString().ToLowerInvariant()}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                TeleportCharacterAction data = (TeleportCharacterAction)Definition;
                CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                if (character.Motor is not ICharacterMotorControl motor) { Fail("Motor cannot teleport."); return; }
                if (data.resetMotor) character.Motor.ResetMotor();
                motor.Teleport(data.relative ? character.Transform.position + data.position : data.position);
            }
        }
    }
}
