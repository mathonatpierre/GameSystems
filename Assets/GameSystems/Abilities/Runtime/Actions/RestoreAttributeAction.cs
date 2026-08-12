using System;
using GameSystems.Sequencing;
using GameSystems.Stats;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class RestoreAttributeAction : GameAction
    {
        [SerializeField, Tooltip("Attribute restored to its current calculated maximum.")] AttributeDefinition attribute;
        public override string Summary => $"Restore {(attribute != null ? attribute.DisplayName : "missing attribute")} to maximum";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                RestoreAttributeAction data = (RestoreAttributeAction)Definition;
                CharacterStats stats = Context.Get<CharacterRuntimeContext>().Resolve<CharacterStats>();
                RuntimeAttribute runtime = stats?.GetAttribute(data.attribute);
                if (runtime == null || !stats.Set(data.attribute, runtime.Maximum)) Fail("Attribute is unavailable.");
            }
        }
    }
}
