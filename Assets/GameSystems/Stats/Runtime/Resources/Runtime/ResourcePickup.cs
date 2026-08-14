using GameSystems.Hooks;
using GameSystems.Feedbacks;
using GameSystems.Sequencing;
using GameSystems.Stats.Actions;
using GameSystems.Stats.Conditions;
using UnityEngine;
using UnityEngine.Events;

namespace GameSystems.Stats
{
    [DisallowMultipleComponent]
    public sealed class ResourcePickup : MonoBehaviour
    {
        [SerializeField] GameActionSequence contactSequence = new();
        [SerializeField] UnityEvent collected;
        [SerializeField] FeedbackSequence collectionFeedback;
        bool consumed;

        public void Configure(ResourceDefinition definition, int value, HookId hook)
        {
            contactSequence.Configure(
                new GameCondition[] { new TriggerContactHookCondition(hook) },
                new GameAction[]
                {
                    new AddContactResourceAction(definition, value),
                    new SetGameObjectActiveAction(false)
                });
        }

        public void ConfigureFeedback(FeedbackSequence value) => collectionFeedback = value;

        void OnTriggerEnter(Collider other)
        {
            if (consumed || other == null) return;
            GameActionContext context = new(gameObject, this, other, other.gameObject);
            if (!contactSequence.CanRun(context)) return;
            GameActionRunner runner = contactSequence.CreateRunner(context);
            consumed = true;
            FeedbackService.Play(collectionFeedback, FeedbackContext.From(gameObject)
                .WithPosition(transform.position).WithTarget(other.gameObject));
            runner.Start();
            collected?.Invoke();
        }
    }
}
