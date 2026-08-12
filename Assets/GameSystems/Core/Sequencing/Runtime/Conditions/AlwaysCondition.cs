using System;
using UnityEngine;

namespace GameSystems.Actions
{
    [Serializable]
    public sealed class AlwaysCondition : GameCondition
    {
        [SerializeField, Tooltip("Constant result returned by this condition.")] bool result = true;
        public override string Summary => result ? "Always true" : "Always false";
        protected override bool OnEvaluate(in GameActionContext context) => result;
    }
}
