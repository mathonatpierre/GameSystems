namespace GameSystems.Abilities
{
    public interface IAbilityInputState
    {
        bool IsHeld(AbilityDefinition ability);
        bool AnyAbilityHeld { get; }
    }
}
