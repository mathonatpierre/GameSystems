using System;
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

        public PlayFeedbackAction() { }
        public PlayFeedbackAction(FeedbackSequence sequence, float value = 1f)
        { feedback = sequence; intensity = Mathf.Max(0f, value); }

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
                GameObject source = GameActionContextUtility.OwnerGameObject(Context);
                if (source == null) { Fail("Missing feedback source."); return; }
                Animator animator = source.GetComponentInChildren<Animator>();
                Renderer renderer = source.GetComponentInChildren<Renderer>();
                GameObject visuals = animator != null ? animator.gameObject
                    : renderer != null ? renderer.gameObject : source;
                var feedback = new FeedbackRuntimeContext
                {
                    Position = source.transform.position,
                    Rotation = source.transform.rotation,
                    Intensity = data.intensity
                };
                feedback.Bind("Character", source);
                feedback.Bind("Visual", visuals);
                feedback.Bind("Visuals", visuals);
                feedback.Bind("Slime", visuals);
                FeedbackService.Play(data.feedback, Context.WithValue(feedback));
            }
        }
    }
}
