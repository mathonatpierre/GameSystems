using System;
using GameSystems.Sequencing;
using UnityEngine;

using GameSystems.Characters;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class StompAction : GameAction
    {
        [SerializeField, Min(0f), Tooltip("Immediate downward velocity applied by the stomp.")] float downwardSpeed = 15f;
        public override string Summary => $"Stomp downward at {downwardSpeed:0.##}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            bool needsImpulse;
            StompAction Data => (StompAction)Definition;
            protected override void OnEnter() { base.OnEnter(); needsImpulse = true; }
            protected override bool Tick(float deltaTime)
            {
                ICharacterMotor motor = Context.Get<CharacterRuntimeContext>().Motor;
                if (motor == null) { Fail("Missing character motor."); return true; }
                if (!needsImpulse) return false;
                CharacterMotorCommands commands = motor.Commands;
                commands.HasVerticalOverride = true;
                commands.VerticalOverride = -Data.downwardSpeed;
                motor.Commands = commands;
                needsImpulse = false;
                return false;
            }
            protected override bool TickLate()
            {
                CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                return Context.Get<AbilityRuntime>().ActiveTime > .02f && character.Motor.Result.JustLanded;
            }
        }
    }
}
