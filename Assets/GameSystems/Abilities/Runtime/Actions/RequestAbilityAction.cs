using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class RequestAbilityAction : GameAction
    {
        [SerializeField, Tooltip("Ability requested from the current character controller.")] AbilityDefinition ability;
        [SerializeField, Tooltip("Numeric request payload passed to the ability.")] float value = 1f;
        public override string Summary => $"Request ability {(ability != null ? ability.name : "missing")}, value = {value:0.##}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                RequestAbilityAction data = (RequestAbilityAction)Definition;
                CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                if (character.Abilities == null || !character.Abilities.Request(data.ability, character.Owner, data.value)) Fail("Ability request was rejected.");
            }
        }
    }
}
