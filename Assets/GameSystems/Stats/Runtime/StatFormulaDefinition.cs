using UnityEngine;

namespace GameSystems.Stats
{
    [CreateAssetMenu(menuName = "Game Systems/Stats/Formula", fileName = "FORMULA_")]
    public sealed class StatFormulaDefinition : ScriptableObject
    {
        [SerializeField] string id = "formula";
        [SerializeField] string displayName = "Formula";
        [SerializeField, TextArea] string description;
        [SerializeField] StatFormula formula = new();

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public StatFormula Formula => formula;

        public bool TryEvaluate(IStatProvider stats, out float value, out string error)
            => formula.TryEvaluate(stats, out value, out error);
    }
}
