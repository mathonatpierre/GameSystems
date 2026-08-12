using System;
using GameSystems.Sequencing;
using GameSystems.Stats;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class ModifyAttributeAction : GameAction
    {
        [SerializeField, Tooltip("Attribute changed on the character stats component.")] AttributeDefinition attribute;
        [SerializeField, Tooltip("Signed amount added to the current value.")] float delta = 1f;
        public override string Summary => $"Modify {(attribute != null ? attribute.DisplayName : "missing attribute")} by {delta:+0.##;-0.##;0}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                ModifyAttributeAction data = (ModifyAttributeAction)Definition;
                CharacterStats stats = Context.Get<CharacterRuntimeContext>().Resolve<CharacterStats>();
                if (stats == null || !stats.Change(data.attribute, data.delta)) Fail("Attribute is unavailable.");
            }
        }
    }
}
