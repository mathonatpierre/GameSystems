using System;
using GameSystems.Abilities;
using GameSystems.Sequencing;
using UnityEngine;

using GameSystems.Characters;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class RespawnAtCheckpointAction : GameAction
    {
        [SerializeField, Tooltip("Stop every active ability except the sequence that performs this respawn.")]
        bool resetOtherAbilities = true;

        public override string Summary => $"Respawn at checkpoint, reset abilities = {resetOtherAbilities.ToString().ToLowerInvariant()}";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                RespawnAtCheckpointAction data = (RespawnAtCheckpointAction)Definition;
                CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                AbilityRuntime ability = Context.Get<AbilityRuntime>();
                if (data.resetOtherAbilities) character.Abilities?.ResetForRespawn(ability);
                character.Resolve<ICharacterCheckpointService>()?.Respawn();
            }
        }
    }
}
