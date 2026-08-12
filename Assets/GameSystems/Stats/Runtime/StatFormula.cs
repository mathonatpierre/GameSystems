using System;
using UnityEngine;

namespace GameSystems.Stats
{
    [Serializable]
    public sealed class StatFormula
    {
        [SerializeField, TextArea(1, 3)] string expression = "[FOR] * 2 - [DEF]";

        public string Expression => expression ?? string.Empty;

        public bool TryEvaluate(IStatProvider stats, out float value, out string error)
            => StatFormulaEvaluator.TryEvaluate(Expression, stats, out value, out error);
    }
}
