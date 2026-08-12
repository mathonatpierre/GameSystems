namespace GameSystems.Abilities
{
    public interface ICharacterCommandSource
    {
        void CollectCommands(CharacterRuntimeContext context, CharacterRequestBuffer requests);
    }
}
