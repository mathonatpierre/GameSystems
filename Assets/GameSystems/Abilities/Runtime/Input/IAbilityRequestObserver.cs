namespace GameSystems.Abilities
{
    public interface IAbilityRequestObserver
    {
        void OnAbilityRequestResolved(in AbilityRequest request, AbilityRequestResult result);
    }
}
