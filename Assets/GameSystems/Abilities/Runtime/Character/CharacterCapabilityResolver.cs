using GameSystems.Abilities.Actions;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Abilities
{
    public static class CharacterCapabilityResolver
    {
        readonly struct LocomotionData
        {
            public readonly float Speed, Acceleration, Gravity;
            public LocomotionData(float speed, float acceleration, float gravity)
            { Speed = speed; Acceleration = acceleration; Gravity = gravity; }
        }

        readonly struct JumpData
        {
            public readonly float InitialVelocity, HoldAcceleration, MaximumHoldTime;
            public readonly float FallGravityMultiplier, JumpCutMultiplier, ApexGravityMultiplier, ApexVelocityThreshold;
            public JumpData(float initialVelocity, float holdAcceleration, float maximumHoldTime,
                float fallGravityMultiplier, float jumpCutMultiplier, float apexGravityMultiplier, float apexVelocityThreshold)
            {
                InitialVelocity = initialVelocity; HoldAcceleration = holdAcceleration; MaximumHoldTime = maximumHoldTime;
                FallGravityMultiplier = fallGravityMultiplier; JumpCutMultiplier = jumpCutMultiplier;
                ApexGravityMultiplier = apexGravityMultiplier; ApexVelocityThreshold = apexVelocityThreshold;
            }
        }

        readonly struct WallJumpData
        {
            public readonly float VerticalVelocity, HorizontalVelocity;
            public WallJumpData(float verticalVelocity, float horizontalVelocity)
            { VerticalVelocity = verticalVelocity; HorizontalVelocity = horizontalVelocity; }
        }

        public static bool TryResolve(AbilitySet abilitySet, float standingHeight, float bodyRadius,
            float traversalClearanceMargin, out CharacterMovementCapabilities result)
        {
            result = default;
            if (abilitySet == null) return false;
            LocomotionData? ground = null, air = null;
            JumpData? jump = null;
            WallJumpData? wallJump = null;

            foreach (AbilityDefinition ability in abilitySet.Abilities)
            {
                if (ability is SequenceAbilityDefinition sequence)
                    ReadActions(ability, sequence.Sequence.Actions, ref ground, ref air, ref jump, ref wallJump);
            }

            float gravity = Mathf.Max(.01f, air?.Gravity ?? ground?.Gravity ?? 18.5f);
            float groundSpeed = ground?.Speed ?? air?.Speed ?? 0f;
            float groundAcceleration = ground?.Acceleration ?? 24f;
            float airSpeed = air?.Speed ?? groundSpeed;
            float airAcceleration = air?.Acceleration ?? 24f;
            SimulateJump(jump, gravity, false, out float shortHeight, out float shortTime);
            SimulateJump(jump, gravity, true, out float heldHeight, out float heldTime);
            CharacterJumpTrajectory heldTrajectory = BuildTrajectory(jump, gravity, airSpeed);
            CharacterJumpEnvelope jumpEnvelope = BuildEnvelope(jump, gravity, airSpeed);
            float wallVertical = wallJump?.VerticalVelocity ?? 0f;
            float wallHorizontal = wallJump?.HorizontalVelocity ?? 0f;
            float wallFlightTime = wallVertical > 0f ? 2f * wallVertical / gravity : 0f;
            result = new CharacterMovementCapabilities(
                groundSpeed, groundAcceleration, airSpeed, airAcceleration, gravity,
                shortHeight, heldHeight, airSpeed * shortTime, airSpeed * heldTime,
                wallVertical, wallHorizontal, wallFlightTime, wallHorizontal * wallFlightTime,
                heldTrajectory, jumpEnvelope, Mathf.Max(.1f, standingHeight), Mathf.Max(.02f, bodyRadius),
                Mathf.Max(.1f, standingHeight) + Mathf.Max(0f, traversalClearanceMargin));
            return jump.HasValue || wallJump.HasValue || ground.HasValue || air.HasValue;
        }

        static void ReadActions(AbilityDefinition ability, GameAction[] actions,
            ref LocomotionData? ground, ref LocomotionData? air, ref JumpData? jump, ref WallJumpData? wallJump)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                switch (actions[i])
                {
                    case HorizontalLocomotionAction locomotion:
                        var movement = new LocomotionData(locomotion.MaximumSpeed, locomotion.Acceleration, locomotion.Gravity);
                        if (ability.name.Contains("Air")) air ??= movement; else ground ??= movement;
                        break;
                    case VariableJumpAction variableJump:
                        jump ??= new JumpData(variableJump.InitialVelocity, variableJump.HoldAcceleration,
                            variableJump.MaximumHoldTime, variableJump.FallGravityMultiplier,
                            variableJump.JumpCutMultiplier, variableJump.ApexGravityMultiplier,
                            variableJump.ApexVelocityThreshold);
                        break;
                    case WallJumpAction wall:
                        wallJump ??= new WallJumpData(wall.VerticalVelocity, wall.HorizontalVelocity);
                        break;
                }
            }
        }

        static void SimulateJump(JumpData? source, float baseGravity, bool held,
            out float maximumHeight, out float flightTime)
        {
            maximumHeight = 0f; flightTime = 0f;
            if (!source.HasValue) return;
            JumpData jump = source.Value;
            const float step = 1f / 240f;
            float position = 0f, velocity = jump.InitialVelocity;
            for (int i = 0; i < 2400; i++)
            {
                float time = i * step;
                bool heldThisStep = held && time <= jump.MaximumHoldTime;
                if (i > 0 && heldThisStep) velocity += jump.HoldAcceleration * step;
                float multiplier = velocity < 0f ? jump.FallGravityMultiplier : 1f;
                if (Mathf.Abs(velocity) < jump.ApexVelocityThreshold) multiplier *= jump.ApexGravityMultiplier;
                if (!heldThisStep && velocity > 0f) multiplier *= jump.JumpCutMultiplier;
                velocity -= baseGravity * multiplier * step;
                position += velocity * step;
                maximumHeight = Mathf.Max(maximumHeight, position);
                flightTime = time + step;
                if (time > .05f && position <= 0f && velocity < 0f) break;
            }
        }

        static CharacterJumpEnvelope BuildEnvelope(JumpData? jump, float gravity, float horizontalSpeed)
        {
            if (!jump.HasValue) return default;
            const int sampleCount = 9;
            var trajectories = new CharacterJumpTrajectory[sampleCount];
            var durations = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float hold = jump.Value.MaximumHoldTime * i / (sampleCount - 1f);
                durations[i] = hold;
                trajectories[i] = BuildTrajectory(jump, gravity, horizontalSpeed, hold);
            }
            return new CharacterJumpEnvelope(trajectories, durations);
        }

        static CharacterJumpTrajectory BuildTrajectory(JumpData? source,
            float baseGravity, float horizontalSpeed, float holdDuration = -1f)
        {
            if (!source.HasValue) return default;
            JumpData jump = source.Value;
            const float step = 1f / 240f;
            Vector2[] samples = new Vector2[721];
            float position = 0f, velocity = jump.InitialVelocity;
            int count = 1; samples[0] = Vector2.zero;
            for (int i = 1; i < samples.Length; i++)
            {
                float time = i * step;
                float effectiveHold = holdDuration < 0f ? jump.MaximumHoldTime : holdDuration;
                bool heldThisStep = time <= effectiveHold;
                if (i > 1 && heldThisStep && velocity > 0f) velocity += jump.HoldAcceleration * step;
                float multiplier = velocity < 0f ? jump.FallGravityMultiplier : 1f;
                if (Mathf.Abs(velocity) < jump.ApexVelocityThreshold) multiplier *= jump.ApexGravityMultiplier;
                if (velocity > 0f && !heldThisStep) multiplier *= jump.JumpCutMultiplier;
                velocity -= baseGravity * multiplier * step;
                position += velocity * step;
                samples[count++] = new Vector2(horizontalSpeed * time, position);
                if (position < -4f && velocity < 0f) break;
            }
            if (count != samples.Length) System.Array.Resize(ref samples, count);
            return new CharacterJumpTrajectory(samples);
        }
    }
}
