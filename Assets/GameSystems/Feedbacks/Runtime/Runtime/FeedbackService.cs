using System.Collections.Generic;
using GameSystems.Sequencing;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameSystems.Feedbacks
{
    public static class FeedbackService
    {
        sealed class ActivePlay
        {
            public FeedbackSequence Sequence;
            public GameObject Source;
            public GameActionRunner Runner;
            public int Order;
        }

        static readonly List<ActivePlay> Active = new();
        static FeedbackHost host;
        static int order;

        public static bool Play(FeedbackSequence sequence, in GameActionContext context)
        {
            if (sequence == null || !sequence.Sequence.CanRun(context)) return false;
            RemoveFinished();
            GameObject source = GameActionContextUtility.OwnerGameObject(context);
            List<ActivePlay> matches = Active.FindAll(play => play.Sequence == sequence &&
                (sequence.Concurrency != FeedbackConcurrency.SingletonPerSource || play.Source == source));
            switch (sequence.Concurrency)
            {
                case FeedbackConcurrency.IgnoreWhilePlaying:
                case FeedbackConcurrency.SingletonGlobal:
                case FeedbackConcurrency.SingletonPerSource:
                    if (matches.Count > 0) return false;
                    break;
                case FeedbackConcurrency.RestartExisting:
                    foreach (ActivePlay match in matches) Stop(match);
                    break;
                case FeedbackConcurrency.ReplaceOldest:
                    if (matches.Count >= sequence.MaximumInstances) Stop(Oldest(matches));
                    break;
                case FeedbackConcurrency.LimitInstances:
                    if (matches.Count >= sequence.MaximumInstances) return false;
                    break;
            }

            GameActionRunner runner = sequence.Sequence.CreateRunner(context);
            runner.Start();
            if (!runner.IsRunning && runner.Failed) return false;
            Active.Add(new ActivePlay { Sequence = sequence, Source = source,
                Runner = runner, Order = ++order });
            _ = Host;
            return true;
        }

        public static void Stop(FeedbackSequence sequence, GameObject source = null)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
                if (Active[i].Sequence == sequence && (source == null || Active[i].Source == source))
                    Stop(Active[i]);
        }

        public static void StopAll()
        { for (int i = Active.Count - 1; i >= 0; i--) Stop(Active[i]); }

        static void Tick(bool late)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                ActivePlay play = Active[i];
                bool finished = late ? play.Runner.TickLate() : play.Runner.Tick(Time.deltaTime);
                if (finished) Active.RemoveAt(i);
            }
        }

        static void Stop(ActivePlay play)
        {
            if (play == null) return;
            Active.Remove(play);
            if (play.Runner?.IsRunning == true) play.Runner.Stop();
        }

        static void RemoveFinished()
        { for (int i = Active.Count - 1; i >= 0; i--) if (Active[i].Runner == null || !Active[i].Runner.IsRunning) Active.RemoveAt(i); }

        static ActivePlay Oldest(List<ActivePlay> matches)
        { ActivePlay oldest = matches[0]; foreach (ActivePlay match in matches) if (match.Order < oldest.Order) oldest = match; return oldest; }

        static FeedbackHost Host
        {
            get
            {
                if (host != null) return host;
                GameObject gameObject = new("Game Systems Sequence Service");
                Object.DontDestroyOnLoad(gameObject);
                host = gameObject.AddComponent<FeedbackHost>();
                return host;
            }
        }

        sealed class FeedbackHost : MonoBehaviour
        {
            void OnEnable() => SceneManager.sceneUnloaded += OnSceneUnloaded;
            void OnDisable() => SceneManager.sceneUnloaded -= OnSceneUnloaded;
            void Update() => Tick(false);
            void LateUpdate() => Tick(true);
            static void OnSceneUnloaded(Scene _) => StopAll();
        }
    }
}
