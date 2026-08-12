using System;
using GameSystems.Actions;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class ResetMotorAction : GameAction
    {
        public override string Summary => "Reset character motor";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                ICharacterMotor motor = Context.Get<CharacterRuntimeContext>().Motor;
                if (motor == null) { Fail("Missing character motor."); return; }
                motor.ResetMotor();
            }
        }
    }
}
