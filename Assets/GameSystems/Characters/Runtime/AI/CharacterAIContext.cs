using UnityEngine;
using GameSystems.Abilities;

namespace GameSystems.Characters
{
    public readonly struct CharacterAIContext
    {
        public readonly CharacterRuntimeContext Character;
        public readonly CharacterAIController Controller;
        public readonly CharacterAIBlackboard Blackboard;
        public readonly Transform Target;
        public readonly Vector3 Direction;
        public readonly float Distance;
        public readonly bool HasLineOfSight;

        public CharacterAIContext(CharacterRuntimeContext character, CharacterAIController controller,
            CharacterAIBlackboard blackboard)
        {
            Character = character;
            Controller = controller;
            Blackboard = blackboard;
            Target = blackboard?.Target;
            Distance = blackboard?.Distance ?? 0f;
            Direction = blackboard?.Direction ?? Vector3.zero;
            HasLineOfSight = blackboard?.HasLineOfSight == true;
        }
    }
}
