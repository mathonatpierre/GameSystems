namespace GameSystems.LevelGeneration
{
    public interface ITraversalCapabilityProvider
    {
        bool TryGetJumpCapabilities(out float shortHeight, out float heldHeight,
            out float shortDistance, out float heldDistance);
    }
}
