using UnityEngine;

namespace GameSystems.Abilities
{
    public enum CharacterContactSource { CharacterController, Collision, Trigger, Motor }

    public readonly struct CharacterContactContext
    {
        public readonly CharacterAbilityController Self;
        public readonly CharacterAbilityController Other;
        public readonly Collider OtherCollider;
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public readonly Vector3 RelativeVelocity;
        public readonly CharacterContactSource Source;

        public CharacterContactContext(CharacterAbilityController self, CharacterAbilityController other, Collider otherCollider,
            Vector3 point, Vector3 normal, Vector3 relativeVelocity, CharacterContactSource source)
        {
            Self = self;
            Other = other;
            OtherCollider = otherCollider;
            Point = point;
            Normal = normal;
            RelativeVelocity = relativeVelocity;
            Source = source;
        }

        public bool HasCharacter => Other != null;
        public float TopAlignment => Normal.y;
        public bool IsTopContact(float minimumNormal = .2f) => Normal.y >= minimumNormal;
        public bool IsSideContact(float maximumNormal = .55f) => Mathf.Abs(Normal.y) <= maximumNormal;
    }
}
