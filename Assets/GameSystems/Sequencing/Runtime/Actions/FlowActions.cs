using GameSystems.Sequencing.Values;
using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
        public sealed class DelayAction : GameAction
        {
            [SerializeField, Min(0f), Tooltip("Time to wait before continuing the sequence.")] float duration = .1f;
            [SerializeField, Tooltip("Ignore the gameplay time scale while waiting.")] bool useUnscaledTime;
            public float Duration => duration;
            public bool UseUnscaledTime => useUnscaledTime;
            public DelayAction() { }
            public DelayAction(float duration, bool useUnscaledTime = false)
            { this.duration = Mathf.Max(0f, duration); this.useUnscaledTime = useUnscaledTime; }
            public override string Summary => $"Wait {duration:0.###}s, unscaled = {useUnscaledTime.ToString().ToLowerInvariant()}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : GameActionRuntime
            {
                float elapsed; DelayAction Data => (DelayAction)Definition;
                protected internal override void OnEnter() { base.OnEnter(); elapsed = 0f; }
                protected internal override bool Tick(float deltaTime) { elapsed += Data.UseUnscaledTime ? Time.unscaledDeltaTime : deltaTime; return elapsed >= Data.Duration; }
            }
        }

    [Serializable]
        public sealed class KeepActiveAction : GameAction
        {
            public override string Summary => "Keep sequence active";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : GameActionRuntime
            {
                protected internal override bool Tick(float deltaTime) => false;
            }
        }

    [Serializable]
        public sealed class RunActionSequenceAssetAction : GameAction
        {
            [SerializeField] GameActionSequenceAsset sequence;
            [SerializeField] bool overrideTarget;
            [SerializeReference] GameObjectValue target = new TargetGameObjectValue();
            [SerializeField, Min(1)] int maximumNestingDepth = 16;
    
            public RunActionSequenceAssetAction() { }
            public RunActionSequenceAssetAction(GameActionSequenceAsset sequence) =>
                this.sequence = sequence;
    
            public override string Summary => $"Run {sequence?.name ?? "missing action sequence"}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : GameActionRuntime
            {
                readonly GameActionRunner runner = new();
                RunActionSequenceAssetAction Data => (RunActionSequenceAssetAction)Definition;
    
                protected internal override void OnEnter()
                {
                    base.OnEnter();
                    if (Data.sequence == null)
                    {
                        Fail("Missing action sequence asset.");
                        return;
                    }
                    int depth = Context.TryGet(out ActionSequenceNesting nesting) ? nesting.Depth : 0;
                    if (depth >= Data.maximumNestingDepth)
                    {
                        Fail("Maximum action sequence nesting depth reached.");
                        return;
                    }
                    GameActionContext nestedContext = Data.overrideTarget
                        ? Context.WithTarget(Data.target?.Get(Context)) : Context;
                    nestedContext = nestedContext.WithValue(new ActionSequenceNesting(depth + 1));
                    GameActionSequence nested = Data.sequence.Sequence;
                    if (!nested.CanRun(nestedContext))
                    {
                        Fail("Nested action sequence conditions were rejected.");
                        return;
                    }
                    runner.Initialize(nested.Actions, nestedContext);
                    runner.Start();
                }
    
                protected internal override bool Tick(float deltaTime)
                {
                    if (Failed) return true;
                    bool finished = runner.Tick(deltaTime);
                    if (finished && runner.Failed) Fail("Nested action sequence failed.");
                    return finished;
                }
    
                protected internal override bool TickLate()
                {
                    if (Failed) return true;
                    bool finished = runner.TickLate();
                    if (finished && runner.Failed) Fail("Nested action sequence failed.");
                    return finished;
                }
    
                protected internal override void OnExit()
                {
                    if (runner.IsRunning) runner.Stop();
                    base.OnExit();
                }
            }
    
            readonly struct ActionSequenceNesting
            {
                public ActionSequenceNesting(int depth) => Depth = depth;
                public int Depth { get; }
            }
        }

    [Serializable]
        public sealed class RunConditionalAction : GameAction
        {
            [SerializeReference] GameCondition condition = new AlwaysCondition();
            [SerializeField] GameActionSequence whenTrue = new();
            [SerializeField] GameActionSequence whenFalse = new();
            public override string Summary => $"If {condition?.Summary ?? "missing condition"}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : GameActionRuntime
            {
                readonly GameActionRunner runner = new();
                protected internal override void OnEnter()
                {
                    base.OnEnter();
                    RunConditionalAction data = (RunConditionalAction)Definition;
                    if (data.condition == null) { Fail("Missing branch condition."); return; }
                    GameActionSequence branch = data.condition.Evaluate(Context)
                        ? data.whenTrue : data.whenFalse;
                    if (branch == null || !branch.CanRun(Context)) return;
                    runner.Initialize(branch.Actions, Context);
                    runner.Start();
                }
                protected internal override bool Tick(float deltaTime)
                {
                    if (Failed || !runner.IsRunning) return true;
                    bool finished = runner.Tick(deltaTime);
                    if (finished && runner.Failed) Fail("Conditional branch failed.");
                    return finished;
                }
                protected internal override bool TickLate()
                {
                    if (Failed || !runner.IsRunning) return true;
                    bool finished = runner.TickLate();
                    if (finished && runner.Failed) Fail("Conditional branch failed.");
                    return finished;
                }
                protected internal override void OnExit()
                {
                    if (runner.IsRunning) runner.Stop();
                    base.OnExit();
                }
            }
        }

    [Serializable]
        public sealed class RunParallelAction : GameAction
        {
            [SerializeField] GameActionSequence[] branches = Array.Empty<GameActionSequence>();
            [SerializeField] bool failWhenAnyBranchFails = true;

            public RunParallelAction() { }
            public RunParallelAction(GameActionSequence[] branches,
                bool failWhenAnyBranchFails = true)
            {
                this.branches = branches ?? Array.Empty<GameActionSequence>();
                this.failWhenAnyBranchFails = failWhenAnyBranchFails;
            }
    
            public override string Summary => $"Run {branches?.Length ?? 0} branches in parallel";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : GameActionRuntime
            {
                GameActionRunner[] runners = Array.Empty<GameActionRunner>();
                RunParallelAction Data => (RunParallelAction)Definition;
    
                protected internal override void OnEnter()
                {
                    base.OnEnter();
                    GameActionSequence[] definitions = Data.branches ?? Array.Empty<GameActionSequence>();
                    runners = new GameActionRunner[definitions.Length];
                    for (int i = 0; i < definitions.Length; i++)
                    {
                        GameActionSequence branch = definitions[i];
                        if (branch == null || !branch.CanRun(Context)) continue;
                        runners[i] = branch.CreateRunner(Context);
                        runners[i].Start();
                    }
                }
    
                protected internal override bool Tick(float deltaTime) => TickRunners(deltaTime, false);
                protected internal override bool TickLate() => TickRunners(0f, true);
    
                bool TickRunners(float deltaTime, bool late)
                {
                    bool anyRunning = false;
                    for (int i = 0; i < runners.Length; i++)
                    {
                        GameActionRunner runner = runners[i];
                        if (runner == null || !runner.IsRunning) continue;
                        if (late) runner.TickLate(); else runner.Tick(deltaTime);
                        anyRunning |= runner.IsRunning;
                        if (Data.failWhenAnyBranchFails && runner.Failed)
                        {
                            Fail($"Parallel branch {i + 1} failed.");
                            return true;
                        }
                    }
                    return !anyRunning;
                }
    
                protected internal override void OnExit()
                {
                    for (int i = 0; i < runners.Length; i++)
                        if (runners[i]?.IsRunning == true) runners[i].Stop();
                    base.OnExit();
                }
            }
        }

    [Serializable]
        public sealed class WaitUntilConditionAction : GameAction
        {
            [SerializeReference] GameCondition condition;
            [SerializeField, Min(0f)] float timeout;
            [SerializeField] bool failOnTimeout;
            [SerializeField] bool useUnscaledTime;
    
            public override string Summary =>
                $"Wait until {condition?.Summary ?? "missing condition"}" +
                (timeout > 0f ? $" for max {timeout:0.##}s" : string.Empty);
    
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : GameActionRuntime
            {
                float elapsed;
                WaitUntilConditionAction Data => (WaitUntilConditionAction)Definition;
    
                protected internal override void OnEnter()
                {
                    base.OnEnter();
                    elapsed = 0f;
                    if (Data.condition == null) Fail("Missing condition.");
                }
    
                protected internal override bool Tick(float deltaTime)
                {
                    if (Failed) return true;
                    if (Data.condition.Evaluate(Context)) return true;
                    elapsed += Data.useUnscaledTime ? Time.unscaledDeltaTime : deltaTime;
                    if (Data.timeout <= 0f || elapsed < Data.timeout) return false;
                    if (Data.failOnTimeout) Fail("Wait condition timed out.");
                    return true;
                }
            }
        }
}
