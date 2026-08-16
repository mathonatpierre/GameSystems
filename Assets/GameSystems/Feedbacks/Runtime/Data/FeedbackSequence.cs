using System.Collections.Generic;
using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Feedbacks
{
    [CreateAssetMenu(fileName = "SEQ_", menuName = "Game Systems/Feedbacks/Feedback Sequence")]
    public sealed class FeedbackSequence : ScriptableObject
    {
        [SerializeField] string description;
        [SerializeField] FeedbackConcurrency concurrency = FeedbackConcurrency.AllowMultiple;
        [SerializeField, Min(1)] int maximumInstances = 4;
        [SerializeField] string channel;
        [SerializeField] GameActionSequence actionSequence = new();

        public FeedbackConcurrency Concurrency => concurrency;
        public int MaximumInstances => Mathf.Max(1, maximumInstances);
        public string Channel => channel;
        public string Description => description;
        public GameActionSequence Sequence => actionSequence ??= new GameActionSequence();

        public void Configure(FeedbackConcurrency policy, int limit = 4,
            string sequenceChannel = null)
        {
            concurrency = policy;
            maximumInstances = Mathf.Max(1, limit);
            channel = sequenceChannel;
        }

        public void ReplaceActions(GameAction[] parallelActions)
        {
            var branches = new List<GameActionSequence>();
            foreach (GameAction action in parallelActions ?? Array.Empty<GameAction>())
            {
                if (action == null) continue;
                var branch = new GameActionSequence();
                branch.Configure(Array.Empty<GameCondition>(), new[] { action });
                branches.Add(branch);
            }
            ReplaceWithActions(branches.Count == 0 ? Array.Empty<GameAction>() :
                new GameAction[] { new RunParallelAction(branches.ToArray()) });
        }

        public void ReplaceWithActions(GameAction[] actions)
        {
            actionSequence ??= new GameActionSequence();
            actionSequence.Configure(Array.Empty<GameCondition>(), actions ?? Array.Empty<GameAction>());
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

#if UNITY_EDITOR
        void OnValidate() => maximumInstances = Mathf.Max(1, maximumInstances);
#endif
    }

}
