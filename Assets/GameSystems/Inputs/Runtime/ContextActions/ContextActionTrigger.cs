using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Inputs
{
    [RequireComponent(typeof(Collider), typeof(GameTriggerAction))]
    [DisallowMultipleComponent]
    public sealed class ContextActionTrigger : MonoBehaviour
    {
        [SerializeField] string label = "Interact";
        [SerializeField] int priority;
        [SerializeField] bool oneShot;
        [SerializeField] GameActionSequencePlayer sequence;
        [SerializeField] GameTriggerAction contacts;

        bool consumed;

        public string Label => label;
        public int Priority => priority;

        public void Configure(string displayLabel, int actionPriority,
            GameActionSequencePlayer sequencePlayer, bool executeOnce = false)
        {
            label = string.IsNullOrWhiteSpace(displayLabel) ? "Interact" : displayLabel;
            priority = actionPriority;
            sequence = sequencePlayer;
            oneShot = executeOnce;
            contacts = GetComponent<GameTriggerAction>();
        }

        public bool CanExecute(ContextActionController interactor)
        {
            if (interactor == null || sequence == null || sequence.IsRunning || oneShot && consumed)
                return false;
            GameActionContext context = CreateContext(interactor);
            return sequence.Sequence?.CanRun(context) == true;
        }

        public bool TryExecute(ContextActionController interactor)
        {
            if (!CanExecute(interactor) || !sequence.Play(CreateContext(interactor))) return false;
            consumed = true;
            return true;
        }

        GameActionContext CreateContext(ContextActionController interactor) =>
            new(gameObject, this, interactor, interactor.gameObject);

        void OnEnable()
        {
            if (contacts == null) contacts = GetComponent<GameTriggerAction>();
            if (contacts != null) contacts.Contact += OnContact;
        }

        void OnDisable()
        {
            if (contacts != null) contacts.Contact -= OnContact;
        }

        void OnContact(GameTriggerEvent triggerEvent, Collider other)
        {
            ContextActionController controller = other.GetComponentInParent<ContextActionController>();
            if (controller == null) return;
            if (triggerEvent == GameTriggerEvent.Enter) controller.Register(this);
            else if (triggerEvent == GameTriggerEvent.Exit) controller.Unregister(this);
        }
    }
}
