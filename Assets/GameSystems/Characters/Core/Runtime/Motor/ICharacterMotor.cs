namespace GameSystems.Characters
{
    public interface ICharacterMotor
    {
        CharacterMotorCommands Commands { get; set; }
        CharacterMotorResult Result { get; }
        void StepMotor(float deltaTime);
        void ResetMotor();
    }
}
