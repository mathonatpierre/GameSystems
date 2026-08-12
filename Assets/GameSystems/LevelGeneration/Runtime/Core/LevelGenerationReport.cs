using System.Collections.Generic;

namespace GameSystems.LevelGeneration
{
    public sealed class LevelGenerationReport
    {
        readonly List<string> warnings = new();
        readonly List<string> errors = new();

        public IReadOnlyList<string> Warnings => warnings;
        public IReadOnlyList<string> Errors => errors;
        public bool Succeeded => errors.Count == 0;

        public void Warning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message)) warnings.Add(message);
        }

        public void Error(string message)
        {
            if (!string.IsNullOrWhiteSpace(message)) errors.Add(message);
        }
    }
}
