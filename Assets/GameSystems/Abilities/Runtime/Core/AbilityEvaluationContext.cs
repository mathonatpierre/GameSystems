using GameSystems.Characters;

namespace GameSystems.Abilities
{
    public readonly struct AbilityEvaluationContext
    {
        public readonly CharacterRuntimeContext Character;
        public readonly AbilityRuntime Runtime;
        public readonly AbilityRequest Request;
        public readonly CharacterMotorResult Motor;

        public AbilityEvaluationContext(CharacterRuntimeContext character, AbilityRuntime runtime,
            in AbilityRequest request, in CharacterMotorResult motor)
        { Character = character; Runtime = runtime; Request = request; Motor = motor; }
    }
}
