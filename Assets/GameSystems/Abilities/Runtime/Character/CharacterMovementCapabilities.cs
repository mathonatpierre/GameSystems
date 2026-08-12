namespace GameSystems.Abilities
{
    public readonly struct CharacterMovementCapabilities
    {
        public readonly float GroundSpeed;
        public readonly float GroundAcceleration;
        public readonly float AirSpeed;
        public readonly float AirAcceleration;
        public readonly float Gravity;
        public readonly float ShortJumpHeight;
        public readonly float HeldJumpHeight;
        public readonly float ShortJumpDistance;
        public readonly float HeldJumpDistance;
        public readonly float WallJumpVerticalSpeed;
        public readonly float WallJumpHorizontalSpeed;
        public readonly float WallJumpFlightTime;
        public readonly float WallJumpDistance;
        public readonly CharacterJumpTrajectory HeldJumpTrajectory;
        public readonly CharacterJumpEnvelope JumpEnvelope;
        public readonly float StandingHeight;
        public readonly float BodyRadius;
        public readonly float RequiredClearance;

        public bool HasJump => HeldJumpHeight > 0f;
        public bool HasWallJump => WallJumpVerticalSpeed > 0f && WallJumpHorizontalSpeed > 0f;

        public CharacterMovementCapabilities(float groundSpeed, float groundAcceleration, float airSpeed,
            float airAcceleration, float gravity,
            float shortJumpHeight, float heldJumpHeight, float shortJumpDistance, float heldJumpDistance,
            float wallJumpVerticalSpeed, float wallJumpHorizontalSpeed, float wallJumpFlightTime,
            float wallJumpDistance, CharacterJumpTrajectory heldJumpTrajectory,
            CharacterJumpEnvelope jumpEnvelope, float standingHeight, float bodyRadius,
            float requiredClearance)
        {
            GroundSpeed = groundSpeed;
            GroundAcceleration = groundAcceleration;
            AirSpeed = airSpeed;
            AirAcceleration = airAcceleration;
            Gravity = gravity;
            ShortJumpHeight = shortJumpHeight;
            HeldJumpHeight = heldJumpHeight;
            ShortJumpDistance = shortJumpDistance;
            HeldJumpDistance = heldJumpDistance;
            WallJumpVerticalSpeed = wallJumpVerticalSpeed;
            WallJumpHorizontalSpeed = wallJumpHorizontalSpeed;
            WallJumpFlightTime = wallJumpFlightTime;
            WallJumpDistance = wallJumpDistance;
            HeldJumpTrajectory = heldJumpTrajectory;
            JumpEnvelope = jumpEnvelope;
            StandingHeight = standingHeight;
            BodyRadius = bodyRadius;
            RequiredClearance = requiredClearance;
        }
    }
}
