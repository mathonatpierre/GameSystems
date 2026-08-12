using System;
using UnityEngine;
using UnityEngine.Events;

namespace GameSystems.Sequencing
{
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
}
