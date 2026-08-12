using UnityEngine;

namespace GameSystems.Abilities
{
    public static class CharacterTraversalSolver
    {
        /// <summary>
        /// Single traversal contract shared by procedural generation and runtime agents.
        /// A generated jump is valid if, and only if, the runtime solver accepts it
        /// with this same reserve.
        /// </summary>
        public const float StandardJumpSafety = .94f;

        public static bool CanReachPlatform(in CharacterMovementCapabilities capabilities,
            float horizontalGap, float verticalRise, float safetyMargin)
        {
            if (capabilities.HeldJumpTrajectory.TryGetLandingDistance(verticalRise,
                    out float trajectoryDistance))
                return Mathf.Abs(horizontalGap) <= trajectoryDistance *
                       Mathf.Clamp(safetyMargin, .05f, 1f);

            float horizontal = Mathf.Abs(horizontalGap) /
                               Mathf.Max(.01f, capabilities.HeldJumpDistance);
            float vertical = Mathf.Max(0f, verticalRise) /
                             Mathf.Max(.01f, capabilities.HeldJumpHeight);
            float budget = Mathf.Clamp(safetyMargin, .05f, 1f);
            return horizontal * horizontal + vertical * vertical <= budget * budget;
        }

        public static bool TryCalibrateJump(in CharacterMovementCapabilities capabilities,
            float horizontalGap, float verticalRise, float safetyMargin, out float holdDuration)
            => capabilities.JumpEnvelope.TryFindMinimumHold(horizontalGap, verticalRise,
                safetyMargin, out holdDuration);

        public static bool TryCalibrateRuntimeJump(
            in CharacterMovementCapabilities capabilities,
            float horizontalGap, float verticalRise, float safetyMargin,
            float signedInitialSpeed, out float holdDuration, out float flightTime)
        {
            holdDuration = 0f;
            flightTime = 0f;
            float required = Mathf.Abs(horizontalGap) /
                             Mathf.Clamp(safetyMargin, .05f, 1f);
            for (int i = 0; i < capabilities.JumpEnvelope.Count; i++)
            {
                if (!capabilities.JumpEnvelope.TryGetCandidate(i, verticalRise,
                        out float hold, out _, out float candidateTime)) continue;
                float reach = HorizontalDistanceOverTime(candidateTime,
                    signedInitialSpeed, capabilities.AirSpeed,
                    capabilities.AirAcceleration);
                if (reach < required) continue;
                holdDuration = hold;
                flightTime = candidateTime;
                return true;
            }
            return false;
        }

        public static float HorizontalDistanceOverTime(float time,
            float signedInitialSpeed, float maximumSpeed, float acceleration)
        {
            time = Mathf.Max(0f, time);
            maximumSpeed = Mathf.Max(0f, maximumSpeed);
            acceleration = Mathf.Max(.01f, acceleration);
            float initial = Mathf.Clamp(signedInitialSpeed, -maximumSpeed, maximumSpeed);
            float accelerating = Mathf.Clamp((maximumSpeed - initial) / acceleration, 0f, time);
            float distance = initial * accelerating + .5f * acceleration * accelerating * accelerating;
            distance += maximumSpeed * (time - accelerating);
            return Mathf.Max(0f, distance);
        }

        public static float TimeToTravelDistance(float distance,
            float signedInitialSpeed, float maximumSpeed, float acceleration)
        {
            distance = Mathf.Max(0f, distance);
            if (distance <= .0001f) return 0f;
            maximumSpeed = Mathf.Max(.01f, maximumSpeed);
            acceleration = Mathf.Max(.01f, acceleration);
            float initial = Mathf.Clamp(signedInitialSpeed, -maximumSpeed, maximumSpeed);
            float accelerationTime = Mathf.Max(0f, (maximumSpeed - initial) / acceleration);
            float accelerationDistance = initial * accelerationTime +
                                         .5f * acceleration * accelerationTime * accelerationTime;
            if (distance <= accelerationDistance)
            {
                float discriminant = initial * initial + 2f * acceleration * distance;
                return (-initial + Mathf.Sqrt(Mathf.Max(0f, discriminant))) / acceleration;
            }
            return accelerationTime + (distance - accelerationDistance) / maximumSpeed;
        }

        public static bool CanReachOppositeWall(in CharacterMovementCapabilities capabilities,
            float wallDistance, float requiredRise, float safetyMargin)
        {
            if (!capabilities.HasWallJump || capabilities.Gravity <= 0f) return false;
            float time = Mathf.Abs(wallDistance) /
                         Mathf.Max(.01f, capabilities.WallJumpHorizontalSpeed);
            if (time <= 0f || time >= capabilities.WallJumpFlightTime) return false;
            float rise = capabilities.WallJumpVerticalSpeed * time -
                         .5f * capabilities.Gravity * time * time;
            return rise * Mathf.Clamp01(safetyMargin) >= requiredRise;
        }

        public static float WallRiseAtDistance(in CharacterMovementCapabilities capabilities,
            float wallDistance)
        {
            if (!capabilities.HasWallJump) return 0f;
            float time = Mathf.Abs(wallDistance) /
                         Mathf.Max(.01f, capabilities.WallJumpHorizontalSpeed);
            return capabilities.WallJumpVerticalSpeed * time -
                   .5f * capabilities.Gravity * time * time;
        }
    }
}
