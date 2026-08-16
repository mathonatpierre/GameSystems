using System;
using UnityEngine;
using UnityEngine.Events;

namespace GameSystems.Sequencing
{
    public interface IGameTriggerContactProxy
    {
        GameObject ResolveTriggerTarget(Collider contact);
    }

    [Flags]
    public enum GameTriggerEvent
    {
        Enter = 1,
        Stay = 2,
        Exit = 4
    }

    [DisallowMultipleComponent]
    public sealed class GameTriggerAction : MonoBehaviour
    {
        [SerializeField] GameTriggerEvent events = GameTriggerEvent.Enter;
        [SerializeField, Tooltip("Reusable sequence asset. When empty, the inline sequence is used.")]
        GameActionSequenceAsset sequenceAsset;
        [SerializeField] GameActionSequence inlineSequence = new();
        [SerializeField] bool executeOnce;
        [SerializeField] bool resetOnExit;
        [SerializeField] bool stopOnExit;
        [SerializeField] UnityEvent onCompleted;
        [SerializeField] UnityEvent onRejected;

        readonly GameActionRunner runner = new();
        bool consumed;

        public event Action<GameTriggerEvent, Collider> Contact;
        public event Action Completed;
        public event Action Rejected;

        public GameActionSequence Sequence => sequenceAsset != null ? sequenceAsset.Sequence : inlineSequence;
        public bool IsRunning => runner.IsRunning;

        public void Configure(GameActionSequence sequence, GameTriggerEvent triggerEvents,
            bool once = false, bool rearmOnExit = false)
        {
            inlineSequence = sequence ?? new GameActionSequence();
            sequenceAsset = null;
            events = triggerEvents;
            executeOnce = once;
            resetOnExit = rearmOnExit;
        }

        void Update()
        {
            if (!runner.IsRunning || !runner.Tick(Time.deltaTime)) return;
            NotifyCompleted();
        }

        void LateUpdate()
        {
            if (runner.IsRunning && runner.TickLate()) NotifyCompleted();
        }

        void OnTriggerEnter(Collider other) => TryExecute(GameTriggerEvent.Enter, other);
        void OnTriggerStay(Collider other) => TryExecute(GameTriggerEvent.Stay, other);

        void OnTriggerExit(Collider other)
        {
            if (stopOnExit && runner.IsRunning) runner.Stop();
            if (resetOnExit) consumed = false;
            TryExecute(GameTriggerEvent.Exit, other);
        }

        public bool TryExecute(GameTriggerEvent triggerEvent, Collider other)
        {
            if ((events & triggerEvent) == 0 || other == null)
                return false;
            Contact?.Invoke(triggerEvent, other);
            if (executeOnce && consumed || runner.IsRunning) return false;
            GameObject target = ResolveTarget(other);
            GameActionContext context = new(gameObject, gameObject, target, other);
            if (Sequence == null || !Sequence.CanRun(context))
            {
                onRejected?.Invoke();
                Rejected?.Invoke();
                return false;
            }
            consumed = true;
            runner.Initialize(Sequence.Actions, context);
            runner.Start();
            if (!runner.IsRunning) NotifyCompleted();
            return true;
        }

        static GameObject ResolveTarget(Collider contact)
        {
            MonoBehaviour[] behaviours = contact.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not IGameTriggerContactProxy proxy) continue;
                GameObject target = proxy.ResolveTriggerTarget(contact);
                if (target != null) return target;
            }
            return contact.gameObject;
        }

        void NotifyCompleted()
        {
            onCompleted?.Invoke();
            Completed?.Invoke();
        }

        public void ResetTrigger()
        {
            if (runner.IsRunning) runner.Stop();
            consumed = false;
        }

        void OnDisable() => runner.Stop();
    }
}
