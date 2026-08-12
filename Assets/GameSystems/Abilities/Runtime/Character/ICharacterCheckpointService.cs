namespace GameSystems.Abilities
{
    public interface ICharacterCheckpointService
    {
        void Observe(in CharacterMotorResult motor);
        void Respawn();
    }
}
