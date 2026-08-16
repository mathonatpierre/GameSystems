#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using GameSystems.Characters.AI;
using GameSystems.Core.Editor.NodeEditor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameSystems.Characters.AI.Editor
{
    public sealed class CharacterBehaviorTreeEditorWindow : EditorWindow
    {
        CharacterBehaviorTree tree;
        BehaviorTreeGraphView graph;
        IMGUIContainer inspector;
        string selectedId;

        [MenuItem("Game Systems/Characters/AI/Behavior Tree Editor")]
        public static void Open() => GetWindow<CharacterBehaviorTreeEditorWindow>("Behavior Tree");

        void OnEnable()
        {
            rootVisualElement.Clear();
            var toolbar = new Toolbar();
            var field = new ObjectField("Tree")
            { objectType = typeof(CharacterBehaviorTree), value = tree };
            field.RegisterValueChangedCallback(evt => SetTree(evt.newValue as CharacterBehaviorTree));
            toolbar.Add(field);
            toolbar.Add(new ToolbarButton(() => graph?.FrameAll()) { text = "Frame All" });
            rootVisualElement.Add(toolbar);

            var split = new TwoPaneSplitView(0, 760, TwoPaneSplitViewOrientation.Horizontal);
            graph = new BehaviorTreeGraphView(this);
            inspector = new IMGUIContainer(DrawInspector);
            inspector.style.paddingLeft = 8;
            inspector.style.paddingRight = 8;
            split.Add(graph);
            split.Add(inspector);
            rootVisualElement.Add(split);
            rootVisualElement.schedule.Execute(() => graph?.RefreshRuntimeColors()).Every(100);
            if (tree == null && Selection.activeObject is CharacterBehaviorTree selected) tree = selected;
            graph.Load(tree);
        }

        void SetTree(CharacterBehaviorTree value)
        {
            tree = value;
            selectedId = null;
            graph?.Load(tree);
            Repaint();
        }

        internal void Select(string id)
        { selectedId = id; inspector?.MarkDirtyRepaint(); }

        void DrawInspector()
        {
            if (tree == null)
            {
                EditorGUILayout.HelpBox("Select a Character Behavior Tree asset.", MessageType.Info);
                return;
            }
            BehaviorNode node = tree.Find(selectedId);
            if (node == null)
            {
                EditorGUILayout.LabelField(tree.name, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Select a node to edit its data.", MessageType.Info);
                return;
            }

            SerializedObject serialized = new(tree);
            SerializedProperty nodes = serialized.FindProperty("nodes");
            for (int i = 0; i < nodes.arraySize; i++)
            {
                SerializedProperty candidate = nodes.GetArrayElementAtIndex(i);
                SerializedProperty id = candidate.FindPropertyRelative("id");
                if (id == null || id.stringValue != selectedId) continue;
                serialized.Update();
                EditorGUILayout.LabelField(node.Title, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(candidate, true);
                if (serialized.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(tree);
                    graph.Load(tree);
                }
                break;
            }
        }

        sealed class BehaviorTreeGraphView : NodeEditorGraphView
        {
            readonly CharacterBehaviorTreeEditorWindow window;
            CharacterBehaviorTree tree;
            bool loading;

            public BehaviorTreeGraphView(CharacterBehaviorTreeEditorWindow window) =>
                this.window = window;

            public void Load(CharacterBehaviorTree value)
            {
                loading = true;
                ClearGraph();
                tree = value;
                if (tree != null)
                {
                    BehaviorNode[] definitions = tree.Nodes;
                    for (int i = 0; i < definitions.Length; i++)
                    {
                        BehaviorNode node = definitions[i];
                        if (node == null) continue;
                        bool output = node is BehaviorCompositeNode;
                        NodeEditorNodeView view = AddNodeView(node.Id,
                            node.Id == tree.RootId ? $"ROOT · {node.Title}" : node.Title,
                            node.EditorPosition, node.Id != tree.RootId, output);
                        ApplyRuntimeColor(view, node.Id);
                    }
                    for (int i = 0; i < definitions.Length; i++)
                        if (definitions[i] is BehaviorCompositeNode composite)
                            for (int child = 0; child < composite.Children.Count; child++)
                                AddConnection(composite.Id, composite.Children[child]);
                }
                loading = false;
            }

            void ApplyRuntimeColor(NodeEditorNodeView view, string id)
            {
                view.titleContainer.style.backgroundColor = StyleKeyword.Null;
                view.tooltip = null;
                if (!Application.isPlaying) return;
                CharacterAIController controller = UnityEngine.Object
                    .FindObjectsByType<CharacterAIController>(FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .FirstOrDefault(value => value.Definition?.BehaviorTree == tree);
                if (controller?.BehaviorRuntime?.States.TryGetValue(id,
                        out BehaviorNodeRuntimeState state) != true) return;
                Color color = state.Status switch
                {
                    BehaviorStatus.Running => new Color(.78f, .58f, .12f),
                    BehaviorStatus.Success => new Color(.18f, .62f, .28f),
                    BehaviorStatus.Failure => new Color(.72f, .2f, .2f),
                    _ => new Color(.23f, .23f, .23f)
                };
                view.titleContainer.style.backgroundColor = color;
                if (!string.IsNullOrEmpty(state.Message)) view.tooltip = state.Message;
            }

            public void RefreshRuntimeColors()
            {
                foreach (NodeEditorNodeView view in NodeViews)
                    ApplyRuntimeColor(view, view.Id);
            }

            public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
            {
                base.BuildContextualMenu(evt);
                if (tree == null) return;
                Vector2 position = contentViewContainer.WorldToLocal(evt.mousePosition);
                AddCreateAction<SelectorBehaviorNode>(evt, "Create/Selector", position);
                AddCreateAction<SequenceBehaviorNode>(evt, "Create/Sequence", position);
                AddCreateAction<ConditionBehaviorNode>(evt, "Create/Condition", position);
                AddCreateAction<RequestAbilityBehaviorNode>(evt, "Create/Request Ability", position);
                AddCreateAction<InverterBehaviorNode>(evt, "Create/Inverter", position);
                AddCreateAction<WaitBehaviorNode>(evt, "Create/Wait", position);
                AddCreateAction<ActionSequenceBehaviorNode>(evt, "Create/Action Sequence", position);
                AddCreateAction<SubTreeBehaviorNode>(evt, "Create/Subtree", position);
            }

            void AddCreateAction<T>(ContextualMenuPopulateEvent evt, string label,
                Vector2 position) where T : BehaviorNode, new()
            {
                evt.menu.AppendAction(label, _ =>
                {
                    Undo.RecordObject(tree, "Create Behavior Node");
                    var node = new T();
                    node.SetEditorData(typeof(T).Name.Replace("BehaviorNode", string.Empty), position);
                    tree.AddNode(node);
                    EditorUtility.SetDirty(tree);
                    Load(tree);
                });
            }

            protected override void OnConnected(string parentId, string childId)
            {
                if (loading || tree?.Find(parentId) is not BehaviorCompositeNode parent) return;
                Undo.RecordObject(tree, "Connect Behavior Nodes");
                var children = new List<string>(parent.Children);
                if (!children.Contains(childId)) children.Add(childId);
                parent.SetChildren(children);
                EditorUtility.SetDirty(tree);
            }

            protected override void OnDisconnected(string parentId, string childId)
            {
                if (loading || tree?.Find(parentId) is not BehaviorCompositeNode parent) return;
                Undo.RecordObject(tree, "Disconnect Behavior Nodes");
                var children = new List<string>(parent.Children);
                children.Remove(childId);
                parent.SetChildren(children);
                EditorUtility.SetDirty(tree);
            }

            protected override void OnNodeRemoved(string id)
            {
                if (loading || tree == null) return;
                Undo.RecordObject(tree, "Delete Behavior Node");
                tree.RemoveNode(id);
                EditorUtility.SetDirty(tree);
            }

            protected override void OnNodeMoved(string id, Vector2 position)
            {
                if (loading || tree?.Find(id) is not BehaviorNode node) return;
                node.SetEditorData(node.Title, position);
                EditorUtility.SetDirty(tree);
            }

            protected override void OnNodeSelected(string id) => window.Select(id);
        }
    }

    [CustomEditor(typeof(CharacterBehaviorTree))]
    public sealed class CharacterBehaviorTreeInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CharacterBehaviorTree tree = (CharacterBehaviorTree)target;
            EditorGUILayout.LabelField($"{tree.Nodes.Length} nodes", EditorStyles.boldLabel);
            if (GUILayout.Button("Open Behavior Tree Editor"))
            {
                Selection.activeObject = tree;
                CharacterBehaviorTreeEditorWindow.Open();
            }
            DrawDefaultInspector();
        }
    }
}
#endif
