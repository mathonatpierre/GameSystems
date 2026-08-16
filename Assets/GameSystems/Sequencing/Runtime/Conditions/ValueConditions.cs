using GameSystems.Sequencing.Values;
using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
        public sealed class CompareFloatValuesCondition : GameCondition
        {
            [SerializeReference] FloatValue left = new ConstantFloatValue();
            [SerializeField] NumberComparison comparison;
            [SerializeReference] FloatValue right = new ConstantFloatValue();
            public CompareFloatValuesCondition() { }
            public CompareFloatValuesCondition(FloatValue left, NumberComparison comparison,
                FloatValue right)
            { this.left = left; this.comparison = comparison; this.right = right; }
            public override string Summary => $"{left?.Summary ?? "0"} {comparison} {right?.Summary ?? "0"}";
            protected override bool OnEvaluate(in GameActionContext context)
            {
                float a = left?.Get(context) ?? 0f;
                float b = right?.Get(context) ?? 0f;
                return comparison switch
                {
                    NumberComparison.Less => a < b,
                    NumberComparison.LessOrEqual => a <= b,
                    NumberComparison.Equal => Mathf.Approximately(a, b),
                    NumberComparison.GreaterOrEqual => a >= b,
                    NumberComparison.Greater => a > b,
                    NumberComparison.NotEqual => !Mathf.Approximately(a, b),
                    _ => false
                };
            }
        }
    
        [Serializable]
        public sealed class BoolValueCondition : GameCondition
        {
            [SerializeReference] BoolValue value = new ConstantBoolValue();
            [SerializeField] bool expected = true;
            public BoolValueCondition() { }
            public BoolValueCondition(BoolValue value, bool expected = true)
            { this.value = value; this.expected = expected; }
            public override string Summary => $"{value?.Summary ?? "False"} is {expected}";
            protected override bool OnEvaluate(in GameActionContext context) =>
                (value?.Get(context) ?? false) == expected;
        }

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
