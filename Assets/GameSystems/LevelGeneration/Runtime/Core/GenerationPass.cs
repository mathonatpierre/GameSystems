using UnityEngine;

namespace GameSystems.LevelGeneration
{
    public abstract class GenerationPass : ScriptableObject
    {
        [SerializeField] bool enabled = true;
        [SerializeField] int order;

        public bool Enabled => enabled;
        public int Order => order;

        public abstract void Execute(LevelGenerationContext context);
    }
}
