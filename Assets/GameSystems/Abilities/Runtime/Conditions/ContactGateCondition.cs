using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Abilities.Conditions
{
    [Serializable]
    public sealed class ContactGateCondition : GameCondition
    {
        [SerializeField] string channel = "Contact";
        public override string Summary => $"Contact gate {channel} is open";
        protected override bool OnEvaluate(in GameActionContext context) =>
            context.Get<CharacterRuntimeContext>()?.Resolve<IContactGateService>()?.IsOpen(channel) ?? true;
    }
}
