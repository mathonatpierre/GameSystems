using System;
using GameSystems.Actions;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class RequestReactionAction : GameAction
    {
        [SerializeField, Tooltip("Optional direct generic reaction definition.")] ReactionDefinition reaction;
        [SerializeField, Tooltip("Reaction identifier used when no direct definition is assigned.")] ReactionId reactionId = ReactionId.Hit;
        [SerializeField, Tooltip("Custom identifier used when Reaction Id is Custom.")] string customReactionId;
        [SerializeField, Tooltip("Numeric request payload passed to the reaction.")] float value = 1f;
        public override string Summary => $"Request reaction {(reaction != null ? reaction.name : reactionId == ReactionId.Custom ? customReactionId : reactionId.ToString())}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                RequestReactionAction data = (RequestReactionAction)Definition;
                CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                if (character.Abilities == null) { Fail("Missing ability controller."); return; }
                bool accepted = data.reaction != null
                    ? character.Abilities.RequestReaction(data.reaction, data.value, character.Owner)
                    : data.reactionId == ReactionId.Custom
                        ? character.Abilities.RequestReaction(data.customReactionId, data.value, character.Owner)
                        : character.Abilities.RequestReaction(data.reactionId, data.value, character.Owner);
                if (!accepted) Fail("Reaction request was rejected.");
            }
        }
    }
}
