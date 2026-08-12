namespace GameSystems.LevelGeneration
{
    public interface ITraversalValidator
    {
        TraversalValidationResult Validate(LevelGenerationContext context);
    }
}
