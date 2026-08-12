using System;
using UnityEngine;

namespace GameSystems.Actions
{
    public enum NumberComparison { Less, LessOrEqual, Equal, GreaterOrEqual, Greater, NotEqual }

    [Serializable]
    public sealed class NumberComparisonCondition : GameCondition
    {
        [SerializeField, Tooltip("Left-hand numeric value.")] float left;
        [SerializeField, Tooltip("Comparison operator.")] NumberComparison comparison;
        [SerializeField, Tooltip("Right-hand numeric value.")] float right;
        public override string Summary => $"{left:0.###} {comparison} {right:0.###}";
        protected override bool OnEvaluate(in GameActionContext context) => comparison switch
        {
            NumberComparison.Less => left < right,
            NumberComparison.LessOrEqual => left <= right,
            NumberComparison.Equal => Mathf.Approximately(left, right),
            NumberComparison.GreaterOrEqual => left >= right,
            NumberComparison.Greater => left > right,
            NumberComparison.NotEqual => !Mathf.Approximately(left, right),
            _ => false
        };
    }
}
