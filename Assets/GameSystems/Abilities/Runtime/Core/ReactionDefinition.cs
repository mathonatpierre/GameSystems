using UnityEngine;

namespace GameSystems.Abilities
{
    [CreateAssetMenu(menuName = "Game Systems/Abilities/Reaction", fileName = "REACTION_")]
    public class ReactionDefinition : SequenceAbilityDefinition
    {
        public override AbilityCategory Category => AbilityCategory.Reaction;
    }
}
