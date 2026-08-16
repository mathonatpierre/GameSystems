using GameSystems.Sequencing;
using System;
using UnityEngine;

namespace GameSystems.Abilities.Conditions
{
    public enum CharacterContactOrientation { Any, Top, Side }

    [Serializable]
        public sealed class ContactGateCondition : GameCondition
        {
            [SerializeField] string channel = "Contact";
            public override string Summary => $"Contact gate {channel} is open";
            protected override bool OnEvaluate(in GameActionContext context) =>
                context.Get<CharacterRuntimeContext>()?.Resolve<IContactGateService>()?.IsOpen(channel) ?? true;
        }
}
