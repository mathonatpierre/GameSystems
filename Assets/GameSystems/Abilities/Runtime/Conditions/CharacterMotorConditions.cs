using GameSystems.Characters;
using System;
using UnityEngine;

namespace GameSystems.Abilities.Conditions
{
    [Serializable]
        public sealed class GroundedCondition : AbilityCondition
        {
            [SerializeField, Tooltip("Required grounded value. Disable to test for airborne state.")] bool expected = true;
    
            public GroundedCondition() { }
            public GroundedCondition(bool expected = true) => this.expected = expected;
    
            public override string Summary => expected ? "Is grounded" : "Is airborne";
    
            protected override bool EvaluateAbility(in AbilityEvaluationContext context) =>
                context.Character?.Motor != null && context.Motor.Ground.IsGrounded == expected;
        }

    [Serializable]
        public sealed class JumpWindowCondition : AbilityCondition
        {
            [SerializeField, Min(0f), Tooltip("Grace period after leaving ground during which jump remains valid.")] float coyoteTime = .09f;
            [SerializeField, Tooltip("Maximum upward velocity accepted during the coyote window.")] float maximumUpwardVelocity = .12f;
            public override string Summary => $"Grounded or coyote <= {coyoteTime:0.###}s";
            protected override bool EvaluateAbility(in AbilityEvaluationContext context)
            {
                if (context.Character?.Motor == null) return false;
                if (context.Character.Motor is ICharacterLedgeMotor { IsLedgeAnchored: true })
                    return false;
                CharacterMotorResult motor = context.Motor;
                ICharacterGravityFrame gravity = context.Character.Motor as ICharacterGravityFrame;
                Vector3 up = gravity?.UpDirection ?? Vector3.up;
                float upwardVelocity = Vector3.Dot(motor.Velocity, up);
                return motor.Ground.IsGrounded ||
                       (motor.AirTime <= coyoteTime && upwardVelocity <= maximumUpwardVelocity);
            }
        }

    [Serializable]
        public sealed class LedgeAnchoredCondition : AbilityCondition
        {
            public override string Summary => "Character is hanging from a ledge";
    
            protected override bool EvaluateAbility(in AbilityEvaluationContext context) =>
                context.Character?.Motor is ICharacterLedgeMotor { IsLedgeAnchored: true };
        }

    [Serializable]
        public sealed class LedgeAvailableCondition : AbilityCondition
        {
            public override string Summary => "Reachable ledge ahead";
    
            protected override bool EvaluateAbility(in AbilityEvaluationContext context)
            {
                if (context.Character?.Motor is not ICharacterLedgeMotor ledgeMotor) return false;
                return ledgeMotor.IsLedgeAnchored || ledgeMotor.TryFindLedge(out _);
            }
        }

    [Serializable]
        public sealed class MotorStateCondition : AbilityCondition
        {
            [SerializeField, Tooltip("Required character motor state.")] CharacterMotorState state;
            [SerializeField, Tooltip("Vertical velocity tolerance used by rising and descending states.")]
            float verticalDeadZone = .12f;
    
            public MotorStateCondition() { }
            public MotorStateCondition(CharacterMotorState state, float verticalDeadZone = .12f)
            {
                this.state = state;
                this.verticalDeadZone = verticalDeadZone;
            }
    
            public override string Summary => $"Motor state is {state}";
    
            protected override bool EvaluateAbility(in AbilityEvaluationContext context)
            {
                if (context.Character?.Motor == null) return false;
                return state switch
                {
                    CharacterMotorState.Grounded => context.Motor.Ground.IsGrounded,
                    CharacterMotorState.Airborne => !context.Motor.Ground.IsGrounded,
                    CharacterMotorState.Rising => !context.Motor.Ground.IsGrounded && context.Motor.Velocity.y > verticalDeadZone,
                    CharacterMotorState.Descending => !context.Motor.Ground.IsGrounded && context.Motor.Velocity.y <= verticalDeadZone,
                    _ => false
                };
            }
        }

    [Serializable]
        public sealed class WallContactCondition : AbilityCondition
        {
            [SerializeField, Min(0f), Tooltip("Minimum time spent airborne before wall jump is accepted.")] float minimumAirTime = .04f;
            [SerializeField, Range(0f, 1f), Tooltip("Maximum upward component accepted on the wall normal.")] float upwardNormalAllowance = .28f;
            [SerializeField, Range(0f, 1f), Tooltip("Minimum normalized contact height on the character capsule.")]
            float minimumContactHeight = .32f;
            public override string Summary => $"Wall contact >= {minimumContactHeight:P0} after {minimumAirTime:0.###}s airborne";
            protected override bool EvaluateAbility(in AbilityEvaluationContext context)
            {
                CharacterMotorResult motor = context.Motor;
                if (context.Character?.Motor is ICharacterLedgeMotor { IsLedgeAnchored: true })
                    return true;
                ICharacterGravityFrame gravity = context.Character?.Motor as ICharacterGravityFrame;
                Vector3 up = gravity?.UpDirection ?? Vector3.up;
                return context.Character?.Motor != null && !motor.Ground.IsGrounded && motor.AirTime >= minimumAirTime &&
                       motor.Wall.IsTouching && motor.Wall.Height01 >= minimumContactHeight &&
                       Mathf.Abs(Vector3.Dot(motor.Wall.Normal, up)) <= upwardNormalAllowance;
            }
        }
}
