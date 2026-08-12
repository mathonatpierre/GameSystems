using System;
using UnityEngine;

namespace GameSystems.Actions
{
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
}
