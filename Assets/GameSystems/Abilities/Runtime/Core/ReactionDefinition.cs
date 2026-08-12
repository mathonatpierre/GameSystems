using UnityEngine;

namespace GameSystems.Abilities
{
    [CreateAssetMenu(menuName = "Game Systems/Abilities/Reaction", fileName = "REACTION_")]
    public class ReactionDefinition : SequenceAbilityDefinition
    {
        [SerializeField] ReactionId reactionId = ReactionId.Custom;
        [SerializeField] string customReactionId;

        public ReactionId ReactionId => reactionId;
        public string CustomReactionId => customReactionId;
        public bool Matches(ReactionId id) => reactionId == id;
        public bool Matches(string id) =>
            reactionId == ReactionId.Custom && !string.IsNullOrWhiteSpace(id) && customReactionId == id;
        public override AbilityCategory Category => AbilityCategory.Reaction;
    }
}
