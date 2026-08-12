namespace GameSystems.Abilities
{
    public enum AbilityRequestResult
    {
        Accepted, MissingAbility, NotInAbilitySet, AlreadyActive,
        OnCooldown, RejectedByRuntime, InterruptionBlocked,
        LowerAuthorityPriority
    }
}
