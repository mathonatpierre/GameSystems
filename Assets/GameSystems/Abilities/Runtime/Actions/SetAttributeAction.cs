using System;
using GameSystems.Actions;
using GameSystems.Stats;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class SetAttributeAction : GameAction
    {
        [SerializeField, Tooltip("Attribute set on the character stats component.")] AttributeDefinition attribute;
        [SerializeField, Tooltip("New current value, clamped by the attribute range.")] float value;
        public override string Summary => $"Set {(attribute != null ? attribute.DisplayName : "missing attribute")} = {value:0.##}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetAttributeAction data = (SetAttributeAction)Definition;
                CharacterStats stats = Context.Get<CharacterRuntimeContext>().Resolve<CharacterStats>();
                if (stats == null || !stats.Set(data.attribute, data.value)) Fail("Attribute is unavailable.");
            }
        }
    }
}
