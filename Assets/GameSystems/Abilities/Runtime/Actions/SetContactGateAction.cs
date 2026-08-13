using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class SetContactGateAction : GameAction
    {
        [SerializeField] string channel = "Contact";
        [SerializeField, Min(0f)] float duration = .18f;
        public override string Summary => $"Close contact gate {channel} for {duration:0.##}s";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetContactGateAction data = (SetContactGateAction)Definition;
                IContactGateService gates = Context.Get<CharacterRuntimeContext>()?.Resolve<IContactGateService>();
                if (gates == null) { Fail("Contact gate service is unavailable."); return; }
                gates.Close(data.channel, data.duration);
            }
        }
    }
}
