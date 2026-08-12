using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class HorizontalLocomotionAction : GameAction
    {
        [SerializeField, Min(0f), Tooltip("Maximum horizontal movement speed.")] float maximumSpeed = 4.8f;
        [SerializeField, Min(0f), Tooltip("Acceleration toward the target speed.")] float acceleration = 24f;
        [SerializeField, Min(0f), Tooltip("Deceleration when input is released.")] float deceleration = 32f;
        [SerializeField, Min(0f), Tooltip("Acceleration used when reversing direction.")] float turnAcceleration = 46f;
        [SerializeField, Min(0f), Tooltip("Base gravity written to motor commands.")] float gravity = 18.5f;
        [SerializeField, Min(0f), Tooltip("Maximum downward speed.")] float maximumFallSpeed = 24f;
        public float MaximumSpeed => maximumSpeed;
        public float Acceleration => acceleration;
        public float Gravity => gravity;
        public override string Summary => $"Horizontal locomotion, speed = {maximumSpeed:0.##}, accel = {acceleration:0.##}";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : GameActionRuntime
        {
            IHorizontalInputProvider input;
            HorizontalLocomotionAction Data => (HorizontalLocomotionAction)Definition;
            protected override void OnEnter()
            {
                base.OnEnter();
                input = Context.Get<CharacterRuntimeContext>().Resolve<IHorizontalInputProvider>();
            }
            protected override bool Tick(float deltaTime)
            {
                ICharacterMotor motor = Context.Get<CharacterRuntimeContext>().Motor;
                if (motor == null) { Fail("Missing character motor."); return true; }
                CharacterMotorCommands commands = motor.Commands;
                commands.HasHorizontalTarget = true;
                commands.HorizontalTarget = Mathf.Clamp(input?.Horizontal ?? 0f, -1f, 1f) * Data.maximumSpeed;
                commands.GroundAcceleration = Data.acceleration;
                commands.GroundDeceleration = Data.deceleration;
                commands.GroundTurnAcceleration = Data.turnAcceleration;
                commands.AirAcceleration = Data.acceleration;
                commands.AirDeceleration = Data.deceleration;
                commands.AirTurnAcceleration = Data.turnAcceleration;
                commands.Gravity = Data.gravity;
                commands.MaximumFallSpeed = Data.maximumFallSpeed;
                motor.Commands = commands;
                return false;
            }
        }
    }
}
