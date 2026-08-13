using UnityEngine;

namespace GameSystems.Characters
{
    public readonly struct WallContact
    {
        public readonly bool IsTouching;
        public readonly Collider Collider;
        public readonly Vector3 Point;
        public readonly Vector3 Normal;

        public WallContact(bool isTouching, Collider collider, Vector3 point, Vector3 normal)
        {
            IsTouching = isTouching;
            Collider = collider;
            Point = point;
            Normal = normal;
        }
    }
}
