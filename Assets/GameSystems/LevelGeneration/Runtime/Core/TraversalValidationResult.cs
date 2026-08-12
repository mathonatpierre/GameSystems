namespace GameSystems.LevelGeneration
{
    public readonly struct TraversalValidationResult
    {
        public readonly bool IsTraversable;
        public readonly string Report;

        public TraversalValidationResult(bool isTraversable, string report)
        {
            IsTraversable = isTraversable;
            Report = report ?? string.Empty;
        }
    }
}
