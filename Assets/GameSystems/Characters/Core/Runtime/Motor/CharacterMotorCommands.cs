using UnityEngine;
namespace GameSystems.Characters
{
    public struct CharacterMotorCommands
    {
        public bool HasHorizontalTarget; public float HorizontalTarget;
        public bool HasVerticalOverride; public float VerticalOverride;
        public bool HasGravityDirection; public Vector3 GravityDirection;
        public Vector3 AdditiveImpulse; public float GravityMultiplier;
        public float GroundAcceleration, GroundDeceleration, GroundTurnAcceleration;
        public float AirAcceleration, AirDeceleration, AirTurnAcceleration;
        public float Gravity, MaximumFallSpeed;
        public void Reset() { HasHorizontalTarget = false; HorizontalTarget = 0f; HasVerticalOverride = false;
            VerticalOverride = 0f; AdditiveImpulse = Vector3.zero; GravityMultiplier = 1f;
            HasGravityDirection = false; GravityDirection = Vector3.down;
            GroundAcceleration = GroundDeceleration = GroundTurnAcceleration = 0f;
            AirAcceleration = AirDeceleration = AirTurnAcceleration = 0f;
            Gravity = 0f; MaximumFallSpeed = float.PositiveInfinity; }
    }
}
