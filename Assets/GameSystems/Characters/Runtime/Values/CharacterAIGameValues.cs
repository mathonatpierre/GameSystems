using System;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Characters.Values
{
    [Serializable]
    public sealed class AITargetGameObjectValue : GameObjectValue
    {
        public override string Summary => "AI target";
        public override GameObject Get(in GameActionContext context) =>
            context.TryGet(out CharacterAIContext ai) && ai.Target != null
                ? ai.Target.gameObject : null;
    }

    [Serializable]
    public sealed class AITargetDistanceFloatValue : FloatValue
    {
        public override string Summary => "AI target distance";
        public override float Get(in GameActionContext context) =>
            context.TryGet(out CharacterAIContext ai) && ai.Target != null ? ai.Distance : float.PositiveInfinity;
    }
}
