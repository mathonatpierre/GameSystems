using System.Collections.Generic;

namespace GameSystems.Sequencing
{
    public sealed class GameActionBlackboard
    {
        readonly Dictionary<string, object> values = new();

        public void Set<T>(string key, T value)
        {
            if (!string.IsNullOrWhiteSpace(key)) values[key] = value;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (!string.IsNullOrWhiteSpace(key) && values.TryGetValue(key, out object stored) &&
                stored is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }
    }
}
