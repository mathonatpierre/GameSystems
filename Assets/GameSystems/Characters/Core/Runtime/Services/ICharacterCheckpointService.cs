namespace GameSystems.Characters
{
    public interface ICharacterCheckpointService
    {
        void Observe(in CharacterMotorResult motor);
        void Respawn();
    }
}
