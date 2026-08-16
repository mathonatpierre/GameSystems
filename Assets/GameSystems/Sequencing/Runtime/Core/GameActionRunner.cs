using System;
using UnityEngine;
namespace GameSystems.Sequencing
{
    public sealed class GameActionRunner
    {
        GameActionRuntime[] actions = Array.Empty<GameActionRuntime>(); int index = -1;
        public int CurrentIndex => index; public bool IsRunning => index >= 0 && index < actions.Length;
        public bool Failed { get; private set; }
        public GameAction Current => IsRunning ? actions[index]?.Definition : null;
        public void Initialize(GameAction[] definitions, in GameActionContext context)
        {
            Failed = false;
            GameActionContext runtimeContext = context.TryGet(out GameActionBlackboard _)
                ? context : context.WithValue(new GameActionBlackboard());
            definitions ??= Array.Empty<GameAction>(); actions = new GameActionRuntime[definitions.Length];
            for (int i=0;i<definitions.Length;i++) { actions[i]=definitions[i]?.Enabled == true ? definitions[i].CreateRuntime() : null; actions[i]?.Initialize(definitions[i], runtimeContext); }
            index=-1;
        }
        public void Start()
        {
            Stop();
            Failed = false;
            for (int i = 0; i < actions.Length; i++)
            {
                actions[i]?.Definition.SetDebugStatus(GameActionDebugStatus.Idle);
                actions[i]?.Definition.SetDebugMessage(null);
            }
            index = -1;
            Advance();
            DrainInstantActions();
        }

        public bool Tick(float deltaTime)
        {
            DrainInstantActions();
            if (!IsRunning) return true;
            if (!TickCurrent(deltaTime)) return !IsRunning;
            if (!IsRunning) return true;
            if (actions[index].Failed) { FailCurrent(); return true; }
            CompleteCurrent();
            Advance();
            DrainInstantActions();
            return !IsRunning;
        }

        public bool TickLate()
        {
            if (!IsRunning) return true;
            bool completed;
            try { completed = actions[index].TickLate(); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                actions[index].Definition.SetDebugStatus(GameActionDebugStatus.Failed);
                actions[index].Definition.SetDebugMessage(exception.Message);
                Failed = true;
                index = actions.Length;
                return true;
            }
            if (!completed) return false;
            if (actions[index].Failed) { FailCurrent(); return true; }
            CompleteCurrent();
            Advance();
            DrainInstantActions();
            return !IsRunning;
        }
        public void Stop()
        {
            if (IsRunning)
            {
                actions[index].OnExit();
                actions[index].Definition.SetDebugStatus(GameActionDebugStatus.Failed);
            }
            index=actions.Length;
        }
        void Advance()
        {
            index++; while (index<actions.Length && actions[index]==null) index++;
            if (!IsRunning) return;
            actions[index].Definition.SetDebugStatus(GameActionDebugStatus.Running);
            try { actions[index].OnEnter(); }
            catch (Exception exception) { Debug.LogException(exception); actions[index].Definition.SetDebugStatus(GameActionDebugStatus.Failed); actions[index].Definition.SetDebugMessage(exception.Message); Failed = true; index = actions.Length; }
        }

        void DrainInstantActions()
        {
            while (IsRunning && actions[index] is InstantActionRuntime)
            {
                if (!TickCurrent(0f)) { if (IsRunning) FailCurrent(); return; }
                if (!IsRunning) return;
                if (actions[index].Failed) { FailCurrent(); return; }
                CompleteCurrent();
                Advance();
            }
        }

        bool TickCurrent(float deltaTime)
        {
            try { return actions[index].Tick(deltaTime); }
            catch (Exception exception) { Debug.LogException(exception); actions[index].Definition.SetDebugStatus(GameActionDebugStatus.Failed); actions[index].Definition.SetDebugMessage(exception.Message); Failed = true; index = actions.Length; return false; }
        }

        void CompleteCurrent()
        {
            actions[index].OnExit();
            actions[index].Definition.SetDebugStatus(GameActionDebugStatus.Succeeded);
        }

        void FailCurrent()
        {
            if (!IsRunning) return;
            Failed = true;
            actions[index].OnExit();
            actions[index].Definition.SetDebugStatus(GameActionDebugStatus.Failed);
            actions[index].Definition.SetDebugMessage(
                actions[index].FailureReason ?? "Action failed without a reason.");
            index = actions.Length;
        }
    }
}
