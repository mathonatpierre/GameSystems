using System;
using UnityEngine;
namespace GameSystems.Sequencing
{
    public enum GameConditionDebugStatus { Idle, Evaluating, Succeeded, Failed }

    [Serializable]
    public abstract class GameCondition
    {
        const int MinimumEvaluatingHighlightFrames = 4;
        [SerializeField, Tooltip("Ignore this condition without removing its configuration.")] bool disabled;
        [SerializeField, Tooltip("Invert the evaluated result.")] bool negated;
        [NonSerialized] bool wasEvaluated;
        [NonSerialized] bool debugResult;
        [NonSerialized] int revealResultAtFrame;
        [NonSerialized] string debugMessage;
        public GameConditionDebugStatus DebugStatus => !wasEvaluated
            ? GameConditionDebugStatus.Idle
            : Time.frameCount < revealResultAtFrame
                ? GameConditionDebugStatus.Evaluating
                : debugResult ? GameConditionDebugStatus.Succeeded : GameConditionDebugStatus.Failed;
        public bool HasDebugResult => DebugStatus is GameConditionDebugStatus.Succeeded or GameConditionDebugStatus.Failed;
        public bool DebugResult => debugResult;
        public string DebugMessage => debugMessage;
        public virtual string Summary => GetType().Name;
        public bool Enabled => !disabled;
        public void SetEnabled(bool value) => disabled = !value;
        public bool Evaluate(in GameActionContext context)
        {
            bool result;
            debugMessage = null;
            try { result = OnEvaluate(context); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                debugMessage = exception.Message;
                RecordDebugResult(false);
                return false;
            }
            if (negated) result = !result;
            RecordDebugResult(result);
            return result;
        }
        protected abstract bool OnEvaluate(in GameActionContext context);
        protected internal void RecordDebugResult(bool result)
        {
            if (!wasEvaluated || debugResult != result)
                revealResultAtFrame = Time.frameCount + MinimumEvaluatingHighlightFrames;
            wasEvaluated = true;
            debugResult = result;
        }
        internal void ResetDebugResult() { wasEvaluated = false; debugResult = false; revealResultAtFrame = 0; debugMessage = null; }
    }
}
