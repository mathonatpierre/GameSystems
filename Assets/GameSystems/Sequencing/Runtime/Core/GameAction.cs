using System;
using UnityEngine;
namespace GameSystems.Sequencing
{
    public enum GameActionDebugStatus { Idle, Running, Succeeded, Failed }

    [Serializable]
    public abstract class GameAction
    {
        const int MinimumRunningHighlightFrames = 4;
        [SerializeField, Tooltip("Skip this action without removing its configuration.")] bool disabled;
        [NonSerialized] GameActionDebugStatus debugStatus;
        [NonSerialized] GameActionDebugStatus pendingDebugStatus;
        [NonSerialized] int revealResultAtFrame;
        [NonSerialized] string debugMessage;
        public GameActionDebugStatus DebugStatus =>
            debugStatus == GameActionDebugStatus.Running &&
            pendingDebugStatus is GameActionDebugStatus.Succeeded or GameActionDebugStatus.Failed &&
            Time.frameCount >= revealResultAtFrame
                ? pendingDebugStatus
                : debugStatus;
        public string DebugMessage => debugMessage;
        public virtual string Summary => GetType().Name;
        public bool Enabled => !disabled;
        public void SetEnabled(bool value) => disabled = !value;
        public abstract GameActionRuntime CreateRuntime();
        internal void SetDebugStatus(GameActionDebugStatus value)
        {
            if (value == GameActionDebugStatus.Idle)
            {
                debugStatus = value;
                pendingDebugStatus = GameActionDebugStatus.Idle;
                revealResultAtFrame = 0;
                return;
            }

            if (value == GameActionDebugStatus.Running)
            {
                debugStatus = value;
                pendingDebugStatus = GameActionDebugStatus.Idle;
                revealResultAtFrame = Time.frameCount + MinimumRunningHighlightFrames;
                return;
            }

            if (debugStatus == GameActionDebugStatus.Running)
            {
                pendingDebugStatus = value;
                return;
            }

            debugStatus = value;
        }

        internal void SetDebugMessage(string value) => debugMessage = value;
    }
}
