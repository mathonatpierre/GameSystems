#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using GameSystems.Characters.AI;
using GameSystems.Core.Editor.NodeEditor;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Editor;
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
        ObjectField treeField;
        SerializedObject treeSerialized;
        string selectedId;

        [MenuItem("Game Systems/Characters/AI/Behavior Tree Editor")]
        public static void Open() => GetWindow<CharacterBehaviorTreeEditorWindow>("Behavior Tree");

        void OnEnable()
        {
            rootVisualElement.Clear();
            var toolbar = new Toolbar();
            treeField = new ObjectField("Tree")
            { objectType = typeof(CharacterBehaviorTree), value = tree };
            treeField.RegisterValueChangedCallback(evt => SetTree(evt.newValue as CharacterBehaviorTree));
            toolbar.Add(treeField);
            toolbar.Add(new ToolbarButton(() => graph?.FrameAll()) { text = "Frame All" });
            toolbar.Add(new ToolbarButton(AutoLayout) { text = "Auto Layout" });
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
            treeSerialized = tree != null ? new SerializedObject(tree) : null;
            treeField.SetValueWithoutNotify(tree);
            graph.Load(tree);
        }

        void SetTree(CharacterBehaviorTree value)
        {
            tree = value;
            treeSerialized = tree != null ? new SerializedObject(tree) : null;
            treeField?.SetValueWithoutNotify(tree);
            selectedId = null;
            graph?.Load(tree);
            Repaint();
        }

        void AutoLayout()
        {
            if (tree == null) return;
            Undo.RecordObject(tree, "Auto Layout Behavior Tree");
            tree.AutoLayout();
            EditorUtility.SetDirty(tree);
            graph.Load(tree);
            graph.FrameAll();
        }

        internal void Select(string id)
        { selectedId = id; inspector?.MarkDirtyRepaint(); }

        internal void DrawNodeInline(string id)
        {
            if (tree == null || string.IsNullOrEmpty(id)) return;
            SerializedObject serialized = treeSerialized ??= new SerializedObject(tree);
            serialized.Update();
            SerializedProperty node = FindNodeProperty(serialized, id);
            if (node == null) return;

            bool drewContent = false;
            SerializedProperty conditions = node.FindPropertyRelative("conditions");
            if (conditions != null)
            {
                DrawInlineListBox("Conditions", conditions, typeof(GameCondition));
                drewContent = true;
            }
            SerializedProperty actions = node.FindPropertyRelative("actions");
            if (actions != null)
            {
                if (drewContent) EditorGUILayout.Space(3f);
                DrawInlineListBox("Actions", actions, typeof(GameAction));
                drewContent = true;
            }

            if (drewContent && serialized.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(tree);
                inspector?.MarkDirtyRepaint();
            }
        }

        static void DrawInlineListBox(string title, SerializedProperty property, Type baseType)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{title}  ({property.arraySize})", EditorStyles.miniBoldLabel);
            ManagedReferenceListUtility.DrawLayout(property, baseType);
            EditorGUILayout.EndVertical();
        }

        static SerializedProperty FindNodeProperty(SerializedObject serialized, string id)
        {
            SerializedProperty nodes = serialized.FindProperty("nodes");
            for (int i = 0; i < nodes.arraySize; i++)
            {
                SerializedProperty candidate = nodes.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("id")?.stringValue == id) return candidate;
            }
            return null;
        }

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

            SerializedObject serialized = treeSerialized ??= new SerializedObject(tree);
            serialized.Update();
            SerializedProperty nodes = serialized.FindProperty("nodes");
            for (int i = 0; i < nodes.arraySize; i++)
            {
                SerializedProperty candidate = nodes.GetArrayElementAtIndex(i);
                SerializedProperty id = candidate.FindPropertyRelative("id");
                if (id == null || id.stringValue != selectedId) continue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(node.Kind, EditorStyles.miniBoldLabel);
                SerializedProperty title = candidate.FindPropertyRelative("title");
                EditorGUILayout.PropertyField(title, new GUIContent("Name"));
                if (!string.IsNullOrWhiteSpace(node.Detail))
                    EditorGUILayout.LabelField(node.Detail, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6f);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Node Data", EditorStyles.boldLabel);
                DrawNodeProperties(candidate);
                EditorGUILayout.EndVertical();
                if (serialized.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(tree);
                    graph.Load(tree);
                }
                break;
            }
        }

        static void DrawNodeProperties(SerializedProperty node)
        {
            SerializedProperty property = node.Copy();
            SerializedProperty end = property.GetEndProperty();
            int childDepth = node.depth + 1;
            bool enterChildren = true;
            bool drewField = false;
            while (property.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(property, end))
            {
                enterChildren = false;
                if (property.depth != childDepth) continue;
                if (property.name is "id" or "title" or "editorPosition" or "children") continue;
                if (property.name == "conditions")
                {
                    EditorGUILayout.LabelField("Conditions", EditorStyles.miniBoldLabel);
                    ManagedReferenceListUtility.DrawLayout(property, typeof(GameCondition));
                }
                else if (property.name == "actions")
                {
                    EditorGUILayout.LabelField("Actions", EditorStyles.miniBoldLabel);
                    ManagedReferenceListUtility.DrawLayout(property, typeof(GameAction));
                }
                else EditorGUILayout.PropertyField(property, true);
                drewField = true;
            }
            if (!drewField)
                EditorGUILayout.LabelField("This node has no editable data.", EditorStyles.miniLabel);
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
                            node.Id == tree.RootId ? $"ROOT · {node.EditorLabel}" : node.EditorLabel,
                            node.EditorPosition, node.Id != tree.RootId, output);
                        ApplyTypeVisual(view, node);
                        AddInlineLists(view, node);
                        ApplyRuntimeColor(view, node.Id);
                    }
                    for (int i = 0; i < definitions.Length; i++)
                        if (definitions[i] is BehaviorCompositeNode composite)
                            for (int child = 0; child < composite.Children.Count; child++)
                                AddConnection(composite.Id, composite.Children[child]);
                }
                loading = false;
            }

            void AddInlineLists(NodeEditorNodeView view, BehaviorNode node)
            {
                if (node is not ConditionBehaviorNode && node is not ActionSequenceBehaviorNode)
                    return;
                view.style.width = 380f;
                view.style.minHeight = 130f;
                var content = new IMGUIContainer(() => window.DrawNodeInline(node.Id));
                content.style.marginLeft = 5f;
                content.style.marginRight = 5f;
                content.style.marginTop = 4f;
                content.style.marginBottom = 5f;
                view.extensionContainer.Add(content);
                view.expanded = true;
                view.RefreshExpandedState();
            }

            void ApplyRuntimeColor(NodeEditorNodeView view, string id)
            {
                BehaviorNode node = tree?.Find(id);
                view.titleContainer.style.backgroundColor = TypeColor(node);
                view.tooltip = null;
                if (!Application.isPlaying) return;
                CharacterAIController controller = UnityEngine.Object
                    .FindObjectsByType<CharacterAIController>(FindObjectsInactive.Include)
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

            static void ApplyTypeVisual(NodeEditorNodeView view, BehaviorNode node)
            {
                GUIContent icon = EditorGUIUtility.IconContent(TypeIcon(node));
                view.SetTypeVisual(icon?.image as Texture2D, TypeColor(node));
            }

            static string TypeIcon(BehaviorNode node) => node switch
            {
                SelectorBehaviorNode => "d_FilterByType",
                SequenceBehaviorNode => "d_UnityEditor.AnimationWindow",
                ConditionBehaviorNode => "d_FilterSelectedOnly",
                ActionSequenceBehaviorNode => "d_PlayButton",
                CooldownBehaviorNode => "d_UnityEditor.ProfilerWindow",
                InverterBehaviorNode => "d_Refresh",
                WaitBehaviorNode => "d_PauseButton",
                SubTreeBehaviorNode => "d_UnityEditor.HierarchyWindow",
                _ => "d_UnityEditor.ConsoleWindow"
            };

            static Color TypeColor(BehaviorNode node) => node switch
            {
                SelectorBehaviorNode => new Color(.12f, .42f, .46f),
                SequenceBehaviorNode => new Color(.18f, .34f, .56f),
                ConditionBehaviorNode => new Color(.58f, .43f, .12f),
                ActionSequenceBehaviorNode => new Color(.18f, .46f, .28f),
                CooldownBehaviorNode => new Color(.58f, .3f, .1f),
                InverterBehaviorNode => new Color(.42f, .24f, .52f),
                WaitBehaviorNode => new Color(.34f, .36f, .4f),
                SubTreeBehaviorNode => new Color(.12f, .4f, .54f),
                _ => new Color(.27f, .29f, .32f)
            };

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
                AddCreateAction<InverterBehaviorNode>(evt, "Create/Inverter", position);
                AddCreateAction<CooldownBehaviorNode>(evt, "Create/Cooldown", position);
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
