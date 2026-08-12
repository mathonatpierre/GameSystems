using System;
using GameSystems.Abilities;
using GameSystems.Sequencing;
using GameSystems.Feedbacks;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class PlayFeedbackAction : GameAction
    {
        [SerializeField, Tooltip("Feedback sequence to play.")] FeedbackSequence feedback;
        [SerializeField, Min(0f), Tooltip("Intensity passed to every feedback cue.")] float intensity = 1f;

        public override string Summary => feedback != null
            ? $"Play {feedback.name}, intensity = {intensity:0.##}"
            : "Play feedback (missing)";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                PlayFeedbackAction data = (PlayFeedbackAction)Definition;
                if (data.feedback == null) return;
                CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                Animator animator = character.Owner.GetComponentInChildren<Animator>();
                Renderer renderer = character.Owner.GetComponentInChildren<Renderer>();
                GameObject visuals = animator != null ? animator.gameObject
                    : renderer != null ? renderer.gameObject : character.Owner;
                FeedbackService.Play(data.feedback, FeedbackContext.From(character.Owner)
                    .WithIntensity(data.intensity)
                    .Bind("Character", character.Owner)
                    .Bind("Visual", visuals)
                    .Bind("Visuals", visuals)
                    .Bind("Slime", visuals));
            }
        }
    }
}
