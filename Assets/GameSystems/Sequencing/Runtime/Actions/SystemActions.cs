using System;
using UnityEngine.Events;
using UnityEngine;
using GameSystems.Core;
using GameSystems.Sequencing.Values;

namespace GameSystems.Sequencing
{
    [Serializable]
    public sealed class RaiseGameEventAction : GameAction
    {
        [SerializeField] GameEvent gameEvent;
        [SerializeReference] GameObjectValue sender = new SelfGameObjectValue();
        public RaiseGameEventAction() { }
        public RaiseGameEventAction(GameEvent gameEvent, GameObjectValue sender = null)
        { this.gameEvent = gameEvent; if (sender != null) this.sender = sender; }
        public override string Summary => $"Raise game event [{gameEvent?.name ?? "missing"}]";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                RaiseGameEventAction data = (RaiseGameEventAction)Definition;
                if (data.gameEvent == null) { Fail("Missing game event."); return; }
                data.gameEvent.Raise(data.sender?.Get(Context));
            }
        }
    }

    [Serializable]
        public sealed class InvokeEventAction : GameAction
        {
            [SerializeField, Tooltip("UnityEvent invoked by this action.")] UnityEvent callback = new();
            public override string Summary => $"Invoke event ({callback?.GetPersistentEventCount() ?? 0} persistent listeners)";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute() => ((InvokeEventAction)Definition).callback?.Invoke();
            }
        }

    public enum LogSeverity { Info, Warning, Error }
    
        [Serializable]
        public sealed class LogAction : GameAction
        {
            [SerializeField, Tooltip("Message written to the Unity Console.")] string message = "Action sequence";
            [SerializeField, Tooltip("Console severity used for the message.")] LogSeverity severity;
            public override string Summary => $"Log {severity}: {message}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    LogAction data = (LogAction)Definition;
                    switch (data.severity)
                    {
                        case LogSeverity.Warning: Debug.LogWarning(data.message, Context.Owner); break;
                        case LogSeverity.Error: Debug.LogError(data.message, Context.Owner); break;
                        default: Debug.Log(data.message, Context.Owner); break;
                    }
                }
            }
        }

    [Serializable]
        public sealed class SetTimeScaleAction : GameAction
        {
            [SerializeField, Min(0f), Tooltip("New global Unity time scale.")] float timeScale = 1f;
            public override string Summary => $"Set time scale = {timeScale:0.###}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute() => Time.timeScale = ((SetTimeScaleAction)Definition).timeScale;
            }
        }
}
