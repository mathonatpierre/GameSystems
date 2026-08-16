using System;

namespace GameSystems.Sequencing.Values
{
    [Serializable]
    public abstract class GameValue
    {
        public abstract string Summary { get; }
    }
}
