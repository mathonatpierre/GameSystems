using UnityEngine;
namespace GameSystems.Characters
{
    public readonly struct CharacterMotorResult
    {
        public readonly Vector3 Velocity; public readonly GroundContact Ground; public readonly WallContact Wall; public readonly bool WasGrounded;
        public readonly bool JustLanded; public readonly bool JustLeftGround; public readonly float AirTime; public readonly float ImpactSpeed;
        public CharacterMotorResult(Vector3 velocity, GroundContact ground, WallContact wall, bool wasGrounded, float airTime, float impactSpeed)
        { Velocity = velocity; Ground = ground; Wall = wall; WasGrounded = wasGrounded; JustLanded = !wasGrounded && ground.IsGrounded;
          JustLeftGround = wasGrounded && !ground.IsGrounded; AirTime = airTime; ImpactSpeed = impactSpeed; }
    }
}
