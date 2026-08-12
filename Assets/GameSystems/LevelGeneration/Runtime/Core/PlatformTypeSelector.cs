using System;
using System.Collections.Generic;

namespace GameSystems.LevelGeneration
{
    public static class PlatformTypeSelector
    {
        public static bool CanGenerate(PlatformTypeDefinition definition,
            float normalizedProgress, int occurrences, float distanceFromPrevious)
        {
            if (definition == null || !definition.Selection.Enabled) return false;
            PlatformSelectionRules rules = definition.Selection;
            return occurrences < rules.MaximumOccurrences &&
                   normalizedProgress >= rules.MinimumLevelProgress &&
                   normalizedProgress <= rules.MaximumLevelProgress &&
                   distanceFromPrevious >= rules.MinimumDistanceFromPrevious;
        }

        public static bool ShouldGenerate(PlatformTypeDefinition definition,
            float normalizedProgress, int occurrences, float distanceFromPrevious,
            double randomValue)
        {
            if (!CanGenerate(definition, normalizedProgress, occurrences,
                    distanceFromPrevious)) return false;
            if (occurrences < definition.Selection.MinimumOccurrences) return true;
            return randomValue < definition.Selection.Frequency;
        }

        public static PlatformTypeDefinition PickWeighted(
            IReadOnlyList<PlatformTypeDefinition> candidates, float normalizedProgress,
            Func<PlatformTypeDefinition, int> occurrenceCount,
            Func<PlatformTypeDefinition, float> distanceFromPrevious,
            double randomValue)
        {
            float total = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                PlatformTypeDefinition item = candidates[i];
                if (CanGenerate(item, normalizedProgress, occurrenceCount(item),
                        distanceFromPrevious(item))) total += item.Selection.Weight;
            }
            if (total <= 0f) return null;
            float cursor = (float)randomValue * total;
            for (int i = 0; i < candidates.Count; i++)
            {
                PlatformTypeDefinition item = candidates[i];
                if (!CanGenerate(item, normalizedProgress, occurrenceCount(item),
                        distanceFromPrevious(item))) continue;
                cursor -= item.Selection.Weight;
                if (cursor <= 0f) return item;
            }
            return null;
        }
    }
}
