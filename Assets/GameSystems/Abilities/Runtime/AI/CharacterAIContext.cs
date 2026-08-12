using UnityEngine;

namespace GameSystems.Abilities
{
    public readonly struct CharacterAIContext
    {
        public readonly CharacterRuntimeContext Character;
        public readonly CharacterAIController Controller;
        public readonly Transform Target;
        public readonly Vector3 Direction;
        public readonly float Distance;
        public readonly bool HasLineOfSight;

        public CharacterAIContext(CharacterRuntimeContext character, CharacterAIController controller,
            Transform target, bool hasLineOfSight)
        {
            Character = character;
            Controller = controller;
            Target = target;
            Vector3 delta = target != null ? target.position - character.Transform.position : Vector3.zero;
            Distance = delta.magnitude;
            Direction = Distance > .0001f ? delta / Distance : Vector3.zero;
            HasLineOfSight = target != null && hasLineOfSight;
        }
    }
}
