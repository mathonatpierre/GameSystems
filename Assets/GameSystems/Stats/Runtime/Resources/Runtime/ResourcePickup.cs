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
                    new AddResourceAction(definition, value),
                    new SetGameObjectActiveAction(false)
                });
        }

        public void ConfigureFeedback(FeedbackSequence value) => collectionFeedback = value;

        void OnTriggerEnter(Collider other) => TryCollect(other);
        void OnTriggerStay(Collider other) => TryCollect(other);

        void TryCollect(Collider other)
        {
            if (consumed || other == null) return;
            GameActionContext context = new(gameObject, this, other.gameObject, other);
            if (!contactSequence.CanRun(context)) return;
            GameActionRunner runner = contactSequence.CreateRunner(context);
            consumed = true;
            var feedback = new FeedbackRuntimeContext
            {
                Position = transform.position,
                Rotation = transform.rotation
            };
            FeedbackService.Play(collectionFeedback,
                new GameActionContext(gameObject, gameObject, other.gameObject, feedback));
            runner.Start();
            collected?.Invoke();
        }
    }
}
