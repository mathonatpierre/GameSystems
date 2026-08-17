using GameSystems.Abilities;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Characters
{
    public sealed class CharacterAIBlackboard
    {
        public sealed class TraversalState
        {
            Transform landingTarget;
            Collider landingCollider;
            ICharacterMovingPlatform movingPlatform;
            Vector3 landingPoint;

            public float LandingDirection { get; private set; }
            public float HoldDuration { get; private set; }
            public float FlightTime { get; private set; }
            public double CreatedAt { get; private set; }
            public Transform IgnoredTargetRoot { get; private set; }
            public bool WallTraversalActive { get; set; }
            public float WallTraversalDirection { get; set; }
            public Collider LandingCollider => landingCollider;
            public Transform LandingTarget => landingTarget;
            public bool LandingIsDynamicTarget => landingTarget != null;
            public bool HasLanding => landingTarget != null || landingCollider != null &&
                landingCollider.enabled && landingCollider.gameObject.activeInHierarchy;

            public void SetLanding(Transform target, Collider collider,
                ICharacterMovingPlatform moving, Vector3 point, float hold, float flight,
                float characterX)
            {
                landingTarget = target;
                landingCollider = collider;
                movingPlatform = moving;
                landingPoint = point;
                LandingDirection = Mathf.Sign(point.x - characterX);
                HoldDuration = hold;
                FlightTime = flight;
                CreatedAt = Time.timeAsDouble;
            }

            public bool TryGetLandingX(CharacterAbilityController abilities, out float worldX)
            {
                if (landingTarget != null) { worldX = landingTarget.position.x; return true; }
                if (landingCollider != null)
                {
                    worldX = movingPlatform != null
                        ? landingCollider.bounds.center.x + movingPlatform.PredictDisplacement(
                            Mathf.Max(0f, FlightTime - (abilities?.Motor?.Result.AirTime ?? 0f))).x
                        : landingPoint.x;
                    return true;
                }
                worldX = 0f;
                return false;
            }

            public bool IsIgnoredTarget(Transform target) => target != null &&
                IgnoredTargetRoot != null &&
                (target == IgnoredTargetRoot || target.IsChildOf(IgnoredTargetRoot));

            public void IgnoreTarget(Transform target) => IgnoredTargetRoot = target;

            public void ClearLanding()
            {
                landingTarget = null;
                landingCollider = null;
                movingPlatform = null;
                HoldDuration = 0f;
                FlightTime = 0f;
                LandingDirection = 0f;
            }

            public void PlanAbilityHold(float duration)
            {
                HoldDuration = Mathf.Max(0f, duration);
                CreatedAt = Time.timeAsDouble;
            }
        }

        public TraversalState Traversal { get; } = new();
        public CharacterRuntimeContext Character { get; private set; }
        public Transform Target { get; private set; }
        public Vector3 Direction { get; private set; }
        public float Distance { get; private set; }
        public bool HasLineOfSight { get; private set; }
        public bool IsGrounded { get; private set; }
        public Vector3 Velocity { get; private set; }
        public WallContact Wall { get; private set; }
        public bool HasNearbyWall { get; private set; }
        public float NearbyWallDirection { get; private set; }
        public float HorizontalIntent { get; private set; }
        public AbilityDefinition LastAcceptedAbility { get; private set; }
        public double LastAbilityAcceptedAt { get; private set; }
        readonly Dictionary<AbilityDefinition, double> acceptedAt = new();

        internal void Update(CharacterRuntimeContext character, Transform target,
            bool lineOfSight, bool hasNearbyWall, float nearbyWallDirection)
        {
            Character = character;
            Target = target;
            Vector3 delta = target != null
                ? target.position - character.Transform.position : Vector3.zero;
            Distance = delta.magnitude;
            Direction = Distance > .0001f ? delta / Distance : Vector3.zero;
            HasLineOfSight = target != null && lineOfSight;
            CharacterMotorResult motor = character.Motor.Result;
            IsGrounded = motor.Ground.IsGrounded;
            Velocity = motor.Velocity;
            Wall = motor.Wall;
            HasNearbyWall = hasNearbyWall;
            NearbyWallDirection = nearbyWallDirection;
        }

        public void SetHorizontalIntent(float value) =>
            HorizontalIntent = Mathf.Clamp(value, -1f, 1f);

        internal void RecordAcceptedAbility(AbilityDefinition ability)
        {
            LastAcceptedAbility = ability;
            LastAbilityAcceptedAt = Time.timeAsDouble;
            if (ability != null) acceptedAt[ability] = LastAbilityAcceptedAt;
        }

        public bool WasAcceptedRecently(AbilityDefinition ability, float maximumAge) =>
            ability != null && acceptedAt.TryGetValue(ability, out double time) &&
            Time.timeAsDouble - time <= maximumAge;
    }
}
