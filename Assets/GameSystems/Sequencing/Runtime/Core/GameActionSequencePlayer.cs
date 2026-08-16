using UnityEngine;
using UnityEngine.Events;

namespace GameSystems.Sequencing
{
    public enum GameActionSequencePlayerState { Idle, Running, Completed, Rejected }

    public sealed class GameActionSequencePlayer : MonoBehaviour
    {
        [SerializeField, Tooltip("Reusable sequence asset. When empty, the inline sequence is used.")]
        GameActionSequenceAsset sequenceAsset;
        [SerializeField, Tooltip("Sequence stored directly on this component.")]
        GameActionSequence inlineSequence = new();
        [SerializeField] bool playOnEnable;
        [SerializeField] UnityEvent onCompleted;
        [SerializeField] UnityEvent onRejected;

        readonly GameActionRunner runner = new();

        public GameActionSequencePlayerState State { get; private set; }
        public bool IsRunning => State == GameActionSequencePlayerState.Running;
        public GameActionSequence Sequence => sequenceAsset != null ? sequenceAsset.Sequence : inlineSequence;

        void OnEnable()
        {
            if (playOnEnable) Play();
        }

        void Update()
        {
            if (!IsRunning || !runner.Tick(Time.deltaTime)) return;
            State = GameActionSequencePlayerState.Completed;
            onCompleted?.Invoke();
        }

        public bool Play() => Play(new GameActionContext(gameObject, this, gameObject));

        public bool Play(in GameActionContext context)
        {
            Stop();
            if (Sequence == null || !Sequence.CanRun(context))
            {
                State = GameActionSequencePlayerState.Rejected;
                onRejected?.Invoke();
                return false;
            }

            runner.Initialize(Sequence.Actions, context);
            runner.Start();
            State = runner.IsRunning
                ? GameActionSequencePlayerState.Running
                : GameActionSequencePlayerState.Completed;
            if (State == GameActionSequencePlayerState.Completed) onCompleted?.Invoke();
            return true;
        }

        public void Stop()
        {
            if (runner.IsRunning) runner.Stop();
            State = GameActionSequencePlayerState.Idle;
        }

        void OnDisable() => Stop();
    }
}
