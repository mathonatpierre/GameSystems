using System;
using System.Collections.Generic;
using GameSystems.Abilities;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Characters.AI
{
    public enum BehaviorStatus { Inactive, Running, Success, Failure }

    [CreateAssetMenu(menuName = "Game Systems/Characters/AI/Behavior Tree",
        fileName = "BTREE_")]
    public sealed class CharacterBehaviorTree : ScriptableObject
    {
        [SerializeField] string rootId;
        [SerializeReference] BehaviorNode[] nodes = Array.Empty<BehaviorNode>();

        public string RootId => rootId;
        public BehaviorNode[] Nodes => nodes ?? Array.Empty<BehaviorNode>();

        public void Configure(BehaviorNode root, params BehaviorNode[] values)
        {
            nodes = values ?? Array.Empty<BehaviorNode>();
            rootId = root?.Id;
        }

        public void AddNode(BehaviorNode node)
        {
            if (node == null) return;
            var list = new List<BehaviorNode>(Nodes) { node };
            nodes = list.ToArray();
            if (string.IsNullOrEmpty(rootId)) rootId = node.Id;
        }

        public void RemoveNode(string id)
        {
            var list = new List<BehaviorNode>(Nodes);
            list.RemoveAll(node => node == null || node.Id == id);
            nodes = list.ToArray();
            for (int i = 0; i < nodes.Length; i++)
                if (nodes[i] is BehaviorCompositeNode composite)
                {
                    var children = new List<string>(composite.Children);
                    children.RemoveAll(value => value == id);
                    composite.SetChildren(children);
                }
            if (rootId == id) rootId = nodes.Length > 0 ? nodes[0].Id : null;
        }

        public void SetRoot(string id) => rootId = Find(id) != null ? id : rootId;

        public BehaviorNode Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Nodes.Length; i++)
                if (Nodes[i]?.Id == id) return Nodes[i];
            return null;
        }

        public CharacterBehaviorTreeRuntime CreateRuntime() => new(this);
    }

    public sealed class CharacterBehaviorTreeRuntime
    {
        readonly CharacterBehaviorTree definition;
        readonly Dictionary<string, BehaviorNodeRuntimeState> states = new();
        readonly HashSet<string> evaluating = new();

        public CharacterBehaviorTreeRuntime(CharacterBehaviorTree definition) =>
            this.definition = definition;

        public IReadOnlyDictionary<string, BehaviorNodeRuntimeState> States => states;
        public AbilityDefinition RequestedAbility { get; private set; }
        public string ActiveNodeId { get; private set; }

        public BehaviorStatus Tick(in CharacterAIContext ai, double now)
        {
            RequestedAbility = null;
            ActiveNodeId = null;
            evaluating.Clear();
            BehaviorNode root = definition?.Find(definition.RootId);
            if (root == null) return BehaviorStatus.Failure;
            var context = new BehaviorTreeContext(definition, this, ai, now);
            BehaviorStatus status = Tick(root.Id, context);
            if (status != BehaviorStatus.Running) ResetRunningChildrenExcept(null);
            return status;
        }

        internal BehaviorStatus Tick(string id, in BehaviorTreeContext context)
        {
            BehaviorNode node = definition.Find(id);
            if (node == null) return BehaviorStatus.Failure;
            if (!evaluating.Add(id))
            {
                State(id).Set(BehaviorStatus.Failure, context.Now, "Behavior tree cycle");
                return BehaviorStatus.Failure;
            }
            ActiveNodeId = id;
            BehaviorStatus status = node.Tick(context);
            evaluating.Remove(id);
            State(id).Set(status, context.Now, node.LastMessage);
            return status;
        }

        internal BehaviorNodeRuntimeState State(string id)
        {
            if (!states.TryGetValue(id, out BehaviorNodeRuntimeState state))
            {
                state = new BehaviorNodeRuntimeState();
                states[id] = state;
            }
            return state;
        }

        internal void Request(AbilityDefinition ability) => RequestedAbility = ability;

        public void Reset()
        {
            foreach (BehaviorNodeRuntimeState state in states.Values) state.Reset();
            RequestedAbility = null;
            ActiveNodeId = null;
        }

        void ResetRunningChildrenExcept(string id)
        {
            foreach (KeyValuePair<string, BehaviorNodeRuntimeState> pair in states)
                if (pair.Key != id && pair.Value.Status == BehaviorStatus.Running)
                    pair.Value.Reset();
        }
    }

    public sealed class BehaviorNodeRuntimeState
    {
        public BehaviorStatus Status { get; private set; }
        public double LastTickAt { get; private set; }
        public string Message { get; private set; }
        public int Cursor { get; set; }
        public double Time { get; set; }
        public object Data { get; set; }

        internal void Set(BehaviorStatus status, double now, string message)
        { Status = status; LastTickAt = now; Message = message; }

        public void Reset()
        { Status = BehaviorStatus.Inactive; Message = null; Cursor = 0; Time = 0d; Data = null; }
    }

    public readonly struct BehaviorTreeContext
    {
        public BehaviorTreeContext(CharacterBehaviorTree tree,
            CharacterBehaviorTreeRuntime runtime, CharacterAIContext ai, double now)
        { Tree = tree; Runtime = runtime; AI = ai; Now = now; }

        public CharacterBehaviorTree Tree { get; }
        public CharacterBehaviorTreeRuntime Runtime { get; }
        public CharacterAIContext AI { get; }
        public double Now { get; }
        public GameActionContext Actions => new(AI.Character.Owner, AI,
            AI.Character, AI.Controller);
        public BehaviorStatus Tick(string id) => Runtime.Tick(id, this);
        public BehaviorNodeRuntimeState State(string id) => Runtime.State(id);
    }

    [Serializable]
    public abstract class BehaviorNode
    {
        [SerializeField] string id = Guid.NewGuid().ToString("N");
        [SerializeField] string title;
        [SerializeField] Vector2 editorPosition;
        [NonSerialized] string lastMessage;

        public string Id => id;
        public virtual string Title => string.IsNullOrWhiteSpace(title) ? GetType().Name : title;
        public Vector2 EditorPosition => editorPosition;
        public string LastMessage => lastMessage;
        public abstract BehaviorStatus Tick(in BehaviorTreeContext context);

        public void SetEditorData(string value, Vector2 position)
        { title = value; editorPosition = position; }

        protected BehaviorStatus Fail(string message)
        { lastMessage = message; return BehaviorStatus.Failure; }
        protected BehaviorStatus Result(BehaviorStatus status, string message = null)
        { lastMessage = message; return status; }
    }

    [Serializable]
    public abstract class BehaviorCompositeNode : BehaviorNode
    {
        [SerializeField] List<string> children = new();
        public IReadOnlyList<string> Children => children;
        public void SetChildren(IEnumerable<string> values) =>
            children = values != null ? new List<string>(values) : new List<string>();
    }

    [Serializable]
    public sealed class SelectorBehaviorNode : BehaviorCompositeNode
    {
        public override BehaviorStatus Tick(in BehaviorTreeContext context)
        {
            IReadOnlyList<string> children = Children;
            for (int i = 0; i < children.Count; i++)
            {
                BehaviorStatus status = context.Tick(children[i]);
                if (status != BehaviorStatus.Failure) return Result(status);
            }
            return Result(BehaviorStatus.Failure);
        }
    }

    [Serializable]
    public sealed class SequenceBehaviorNode : BehaviorCompositeNode
    {
        public override BehaviorStatus Tick(in BehaviorTreeContext context)
        {
            IReadOnlyList<string> children = Children;
            for (int i = 0; i < children.Count; i++)
            {
                BehaviorStatus status = context.Tick(children[i]);
                if (status != BehaviorStatus.Success) return Result(status);
            }
            return Result(BehaviorStatus.Success);
        }
    }

    [Serializable]
    public sealed class InverterBehaviorNode : BehaviorCompositeNode
    {
        public override BehaviorStatus Tick(in BehaviorTreeContext context)
        {
            if (Children.Count != 1) return Fail("Inverter requires one child");
            return context.Tick(Children[0]) switch
            {
                BehaviorStatus.Success => Result(BehaviorStatus.Failure),
                BehaviorStatus.Failure => Result(BehaviorStatus.Success),
                BehaviorStatus running => Result(running)
            };
        }
    }

    [Serializable]
    public sealed class ConditionBehaviorNode : BehaviorNode
    {
        [SerializeField] GameConditionMode mode = GameConditionMode.All;
        [SerializeReference] GameCondition[] conditions = Array.Empty<GameCondition>();
        public GameCondition[] Conditions => conditions ?? Array.Empty<GameCondition>();

        public ConditionBehaviorNode Configure(params GameCondition[] values)
        { conditions = values ?? Array.Empty<GameCondition>(); return this; }

        public override BehaviorStatus Tick(in BehaviorTreeContext context)
        {
            bool passed = GameConditionEvaluator.Evaluate(Conditions, mode, context.Actions);
            return Result(passed ? BehaviorStatus.Success : BehaviorStatus.Failure,
                passed ? null : "Condition failed");
        }
    }

    [Serializable]
    public sealed class RequestAbilityBehaviorNode : BehaviorNode
    {
        [SerializeField] AbilityDefinition ability;
        [SerializeField, Min(0f)] float minimumInterval;
        public AbilityDefinition Ability => ability;

        public RequestAbilityBehaviorNode Configure(AbilityDefinition value, float interval = 0f)
        { ability = value; minimumInterval = Mathf.Max(0f, interval); return this; }

        public override BehaviorStatus Tick(in BehaviorTreeContext context)
        {
            if (ability == null) return Fail("Missing ability");
            BehaviorNodeRuntimeState state = context.State(Id);
            if (context.Now < state.Time) return Fail("Cooldown");
            state.Time = context.Now + minimumInterval;
            context.Runtime.Request(ability);
            return Result(BehaviorStatus.Success);
        }
    }

    [Serializable]
    public sealed class WaitBehaviorNode : BehaviorNode
    {
        [SerializeField, Min(0f)] float duration = .25f;
        public WaitBehaviorNode Configure(float value)
        { duration = Mathf.Max(0f, value); return this; }

        public override BehaviorStatus Tick(in BehaviorTreeContext context)
        {
            BehaviorNodeRuntimeState state = context.State(Id);
            if (state.Time <= 0d) state.Time = context.Now + duration;
            if (context.Now < state.Time) return Result(BehaviorStatus.Running);
            state.Time = 0d;
            return Result(BehaviorStatus.Success);
        }
    }

    [Serializable]
    public sealed class ActionSequenceBehaviorNode : BehaviorNode
    {
        [SerializeReference] GameAction[] actions = Array.Empty<GameAction>();
        public override BehaviorStatus Tick(in BehaviorTreeContext context)
        {
            BehaviorNodeRuntimeState state = context.State(Id);
            if (state.Data is not GameActionRunner runner)
            {
                runner = new GameActionRunner();
                runner.Initialize(actions ?? Array.Empty<GameAction>(), context.Actions);
                runner.Start();
                state.Data = runner;
                state.Time = context.Now;
            }
            float delta = Mathf.Max(0f, (float)(context.Now - state.Time));
            state.Time = context.Now;
            if (runner.IsRunning && !runner.Tick(delta)) return Result(BehaviorStatus.Running);
            state.Data = null;
            state.Time = 0d;
            return Result(runner.Failed ? BehaviorStatus.Failure : BehaviorStatus.Success,
                runner.Failed ? "Action sequence failed" : null);
        }
    }

    [Serializable]
    public sealed class SubTreeBehaviorNode : BehaviorNode
    {
        [SerializeField] CharacterBehaviorTree tree;
        public override BehaviorStatus Tick(in BehaviorTreeContext context)
        {
            if (tree == null) return Fail("Missing subtree");
            BehaviorNodeRuntimeState state = context.State(Id);
            CharacterBehaviorTreeRuntime runtime = state.Data as CharacterBehaviorTreeRuntime ??
                                                   tree.CreateRuntime();
            state.Data = runtime;
            BehaviorStatus status = runtime.Tick(context.AI, context.Now);
            if (runtime.RequestedAbility != null) context.Runtime.Request(runtime.RequestedAbility);
            if (status != BehaviorStatus.Running) state.Data = null;
            return Result(status);
        }
    }
}
