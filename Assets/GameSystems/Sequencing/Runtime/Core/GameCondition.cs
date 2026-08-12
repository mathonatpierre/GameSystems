using System;
using UnityEngine;
namespace GameSystems.Sequencing
{
    public enum GameConditionDebugStatus { Idle, Evaluating, Succeeded, Failed }

    [Serializable]
    public abstract class GameCondition
    {
        const int MinimumEvaluatingHighlightFrames = 4;
        [NonSerialized] bool wasEvaluated;
        [NonSerialized] bool debugResult;
        [NonSerialized] int revealResultAtFrame;
        public GameConditionDebugStatus DebugStatus => !wasEvaluated
            ? GameConditionDebugStatus.Idle
            : Time.frameCount < revealResultAtFrame
                ? GameConditionDebugStatus.Evaluating
                : debugResult ? GameConditionDebugStatus.Succeeded : GameConditionDebugStatus.Failed;
        public bool HasDebugResult => DebugStatus is GameConditionDebugStatus.Succeeded or GameConditionDebugStatus.Failed;
        public bool DebugResult => debugResult;
        public virtual string Summary => GetType().Name;
        public bool Evaluate(in GameActionContext context)
        {
            bool result = OnEvaluate(context);
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
        internal void ResetDebugResult() { wasEvaluated = false; debugResult = false; revealResultAtFrame = 0; }
    }
}
