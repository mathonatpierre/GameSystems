using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameSystems.Feedbacks
{
    public static class FeedbackService
    {
        sealed class ActivePlay
        {
            public FeedbackSequence sequence;
            public GameObject source;
            public FeedbackPlayer runner;
            public int order;
        }

        static readonly Queue<FeedbackPlayer> Pool = new();
        static readonly List<ActivePlay> Active = new();
        static FeedbackHost host;
        static int order;

        public static bool Play(FeedbackSequence sequence, FeedbackContext context)
        {
            if (sequence == null) return false;
            context ??= FeedbackContext.From(null);
            RemoveFinished();
            List<ActivePlay> matches = Active.FindAll(play => play.sequence == sequence &&
                (sequence.Concurrency != FeedbackConcurrency.SingletonPerSource || play.source == context.Source));
            switch (sequence.Concurrency)
            {
                case FeedbackConcurrency.IgnoreWhilePlaying:
                case FeedbackConcurrency.SingletonGlobal:
                case FeedbackConcurrency.SingletonPerSource:
                    if (matches.Count > 0) return false;
                    break;
                case FeedbackConcurrency.RestartExisting:
                    foreach (ActivePlay match in matches) Recycle(match);
                    break;
                case FeedbackConcurrency.ReplaceOldest:
                    if (matches.Count >= sequence.MaximumInstances) Recycle(Oldest(matches));
                    break;
                case FeedbackConcurrency.LimitInstances:
                    if (matches.Count >= sequence.MaximumInstances) return false;
                    break;
            }

            FeedbackPlayer runner = Acquire();
            runner.gameObject.SetActive(true);
            runner.PlaySequence(sequence, context);
            var active = new ActivePlay { sequence = sequence, source = context.Source, runner = runner, order = ++order };
            Active.Add(active);
            Host.StartCoroutine(Watch(active));
            return true;
        }

        public static void Stop(FeedbackSequence sequence, GameObject source = null)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
                if (Active[i].sequence == sequence && (source == null || Active[i].source == source)) Recycle(Active[i]);
        }

        public static void StopAll()
        {
            for (int i = Active.Count - 1; i >= 0; i--) Recycle(Active[i]);
        }

        static IEnumerator Watch(ActivePlay play)
        {
            yield return null;
            while (play.runner != null && play.runner.IsPlaying) yield return null;
            if (Active.Contains(play)) Recycle(play);
        }

        static FeedbackPlayer Acquire()
        {
            if (Pool.Count > 0) return Pool.Dequeue();
            var go = new GameObject("Pooled Feedback Runner", typeof(FeedbackPlayer));
            go.transform.SetParent(Host.transform, false);
            return go.GetComponent<FeedbackPlayer>();
        }

        static void Recycle(ActivePlay play)
        {
            if (play == null) return;
            Active.Remove(play);
            if (play.runner == null) return;
            play.runner.StopFeedbacks(true); play.runner.ClearRuntimeContext(); play.runner.gameObject.SetActive(false); Pool.Enqueue(play.runner);
        }

        static ActivePlay Oldest(List<ActivePlay> matches)
        {
            ActivePlay oldest = matches[0];
            foreach (ActivePlay match in matches) if (match.order < oldest.order) oldest = match;
            return oldest;
        }

        static void RemoveFinished()
        {
            for (int i = Active.Count - 1; i >= 0; i--)
                if (Active[i].runner == null || !Active[i].runner.IsPlaying) Recycle(Active[i]);
        }

        static FeedbackHost Host
        {
            get
            {
                if (host != null) return host;
                var go = new GameObject("Game Systems Feedback Service"); Object.DontDestroyOnLoad(go); host = go.AddComponent<FeedbackHost>(); return host;
            }
        }

        sealed class FeedbackHost : MonoBehaviour
        {
            void OnEnable() => SceneManager.sceneUnloaded += OnSceneUnloaded;
            void OnDisable() => SceneManager.sceneUnloaded -= OnSceneUnloaded;
            static void OnSceneUnloaded(Scene _) => StopAll();
        }
    }
}
