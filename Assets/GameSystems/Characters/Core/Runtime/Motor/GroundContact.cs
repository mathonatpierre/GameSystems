using UnityEngine;
namespace GameSystems.Characters
{
    public readonly struct GroundContact
    {
        public readonly bool IsGrounded; public readonly Collider Collider;
        public readonly Vector3 Point; public readonly Vector3 Normal;
        public GroundContact(bool grounded, Collider collider, Vector3 point, Vector3 normal)
        { IsGrounded = grounded; Collider = collider; Point = point; Normal = normal; }
    }
}
