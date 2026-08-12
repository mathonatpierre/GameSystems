using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class WallJumpAction : GameAction
    {
        [SerializeField, Min(0f), Tooltip("Initial upward velocity.")] float verticalVelocity = 6.15f;
        [SerializeField, Min(0f), Tooltip("Horizontal velocity away from the wall.")] float horizontalVelocity = 4.4f;
        [SerializeField, Min(0f), Tooltip("Duration of forced movement away from the wall.")] float horizontalControlLock = .16f;
        [SerializeField, Min(0f), Tooltip("Maximum duration before the action succeeds.")] float completionDelay = .18f;
        public float VerticalVelocity => verticalVelocity;
        public float HorizontalVelocity => horizontalVelocity;
        public override string Summary => $"Wall jump, velocity = ({horizontalVelocity:0.##}, {verticalVelocity:0.##}), lock = {horizontalControlLock:0.###}s";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            float outwardDirection;
            float controlLockRemaining;
            bool needsImpulse;
            WallJumpAction Data => (WallJumpAction)Definition;
            protected override void OnEnter()
            {
                base.OnEnter();
                CharacterMotorResult result = Context.Get<CharacterRuntimeContext>().Motor.Result;
                outwardDirection = Mathf.Abs(result.Wall.Normal.x) > .01f ? Mathf.Sign(result.Wall.Normal.x) : -Mathf.Sign(result.Velocity.x);
                if (Mathf.Approximately(outwardDirection, 0f)) outwardDirection = 1f;
                controlLockRemaining = Data.horizontalControlLock;
                needsImpulse = true;
            }
            protected override bool Tick(float deltaTime)
            {
                ICharacterMotor motor = Context.Get<CharacterRuntimeContext>().Motor;
                if (motor == null) { Fail("Missing character motor."); return true; }
                CharacterMotorCommands commands = motor.Commands;
                if (needsImpulse) { commands.HasVerticalOverride = true; commands.VerticalOverride = Data.verticalVelocity; needsImpulse = false; }
                if (controlLockRemaining > 0f)
                {
                    commands.HasHorizontalTarget = true;
                    commands.HorizontalTarget = outwardDirection * Data.horizontalVelocity;
                    commands.AirAcceleration = Mathf.Max(commands.AirAcceleration, 80f);
                    commands.AirTurnAcceleration = Mathf.Max(commands.AirTurnAcceleration, 80f);
                    controlLockRemaining -= deltaTime;
                }
                motor.Commands = commands;
                return false;
            }
            protected override bool TickLate()
            {
                CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                return character.Motor.Result.JustLanded || Context.Get<AbilityRuntime>().ActiveTime >= Data.completionDelay;
            }
        }
    }
}
