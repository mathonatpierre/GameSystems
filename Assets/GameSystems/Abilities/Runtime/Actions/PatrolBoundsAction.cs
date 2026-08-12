using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class PatrolBoundsAction : GameAction
    {
        [SerializeField, Min(0f)] float speed = 1f;
        [SerializeField, Min(0f)] float acceleration = 8f;
        [SerializeField, Min(0f)] float turnAcceleration = 14f;
        public override string Summary => $"Patrol bounds at {speed:0.##}m/s";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : GameActionRuntime
        {
            ICharacterPatrolArea patrol;
            PatrolBoundsAction Data => (PatrolBoundsAction)Definition;

            protected override void OnEnter()
            {
                base.OnEnter();
                patrol = Context.Get<CharacterRuntimeContext>().Resolve<ICharacterPatrolArea>();
                if (patrol == null) Fail("Missing patrol area provider.");
            }

            protected override bool Tick(float deltaTime)
            {
                CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                ICharacterMotor motor = character.Motor;
                if (patrol == null || motor == null) { Fail("Patrol requires an area and a motor."); return true; }
                float x = character.Transform.position.x;
                if (x >= patrol.MaximumX) patrol.Direction = -1f;
                else if (x <= patrol.MinimumX) patrol.Direction = 1f;
                CharacterMotorCommands commands = motor.Commands;
                commands.HasHorizontalTarget = true;
                commands.HorizontalTarget = Mathf.Sign(patrol.Direction) * Data.speed;
                commands.GroundAcceleration = Data.acceleration;
                commands.GroundDeceleration = Data.acceleration;
                commands.GroundTurnAcceleration = Data.turnAcceleration;
                commands.AirAcceleration = Data.acceleration;
                commands.AirDeceleration = Data.acceleration;
                commands.AirTurnAcceleration = Data.turnAcceleration;
                motor.Commands = commands;
                return false;
            }

        }
    }
}
