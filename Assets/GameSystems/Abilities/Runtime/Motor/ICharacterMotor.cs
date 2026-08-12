namespace GameSystems.Abilities
{
    public interface ICharacterMotor
    {
        CharacterMotorCommands Commands { get; set; }
        CharacterMotorResult Result { get; }
        void StepMotor(float deltaTime);
        void ResetMotor();
    }
}
