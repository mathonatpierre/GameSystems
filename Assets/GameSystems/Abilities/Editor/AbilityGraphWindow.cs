using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Abilities.Editor
{
    public sealed class AbilityGraphWindow : EditorWindow
    {
        const float NodeWidth = 210f;
        const float NodeHeight = 82f;
        const float ColumnGap = 90f;
        const float RowGap = 30f;

        readonly Dictionary<AbilityDefinition, Rect> nodeRects = new();
        readonly Dictionary<AbilityActivationPolicy, int> policyRows = new();
        AbilitySet abilitySet;
        CharacterAbilityController runtimeController;
        Vector2 scroll;

        [MenuItem("Game Systems/Abilities/Ability Graph")]
        static void Open() => GetWindow<AbilityGraphWindow>("Ability Graph");

        void OnEnable() => EditorApplication.playModeStateChanged += OnPlayModeChanged;
        void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        void OnPlayModeChanged(PlayModeStateChange _) => Repaint();

        void OnGUI()
        {
            DrawToolbar();
            if (abilitySet == null)
            {
                EditorGUILayout.HelpBox("Select an Ability Set to inspect its graph.", MessageType.Info);
                return;
            }

            BuildLayout();
            Rect content = CalculateContentRect();
            scroll = GUI.BeginScrollView(
                new Rect(0f, EditorGUIUtility.singleLineHeight + 10f, position.width,
                    position.height - EditorGUIUtility.singleLineHeight - 10f),
                scroll,
                content);

            DrawColumnHeaders(content.height);
            DrawConnections();
            DrawNodes();
            GUI.EndScrollView();

            if (EditorApplication.isPlaying) Repaint();
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                AbilitySet selected = (AbilitySet)EditorGUILayout.ObjectField(
                    abilitySet, typeof(AbilitySet), false, GUILayout.MinWidth(180f));
                if (selected != abilitySet)
                {
                    abilitySet = selected;
                    runtimeController = null;
                }

                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                {
                    CharacterAbilityController selectedController =
                        (CharacterAbilityController)EditorGUILayout.ObjectField(
                            runtimeController, typeof(CharacterAbilityController), true,
                            GUILayout.Width(220f));
                    if (selectedController != runtimeController) runtimeController = selectedController;
                }
            }
        }

        void BuildLayout()
        {
            nodeRects.Clear();
            policyRows.Clear();
            for (int i = 0; i < abilitySet.Abilities.Count; i++)
            {
                AbilityDefinition ability = abilitySet.Abilities[i];
                if (ability == null) continue;
                int column = (int)ability.ActivationPolicy;
                policyRows.TryGetValue(ability.ActivationPolicy, out int row);
                nodeRects[ability] = new Rect(
                    24f + column * (NodeWidth + ColumnGap),
                    62f + row * (NodeHeight + RowGap),
                    NodeWidth,
                    NodeHeight);
                policyRows[ability.ActivationPolicy] = row + 1;
            }
        }

        Rect CalculateContentRect()
        {
            float width = 24f + 4f * (NodeWidth + ColumnGap);
            float height = 180f;
            foreach (Rect rect in nodeRects.Values) height = Mathf.Max(height, rect.yMax + 40f);
            return new Rect(0f, 0f, width, height);
        }

        void DrawColumnHeaders(float height)
        {
            for (int i = 0; i < 4; i++)
            {
                float x = 24f + i * (NodeWidth + ColumnGap);
                Rect column = new(x - 8f, 28f, NodeWidth + 16f, height - 36f);
                EditorGUI.DrawRect(column, i % 2 == 0
                    ? new Color(.12f, .12f, .12f, .22f)
                    : new Color(.2f, .2f, .2f, .15f));
                GUI.Label(new Rect(x, 32f, NodeWidth, 22f),
                    ((AbilityActivationPolicy)i).ToString(), EditorStyles.boldLabel);
            }
        }

        void DrawConnections()
        {
            foreach (KeyValuePair<AbilityDefinition, Rect> pair in nodeRects)
            {
                AbilityDefinition source = pair.Key;
                foreach (AbilityTransitionDefinition transition in source.Transitions)
                {
                    if (transition?.Target == null || !nodeRects.TryGetValue(transition.Target, out Rect target))
                        continue;

                    Rect origin = pair.Value;
                    Vector3 start = new(origin.xMax, origin.center.y);
                    Vector3 end = new(target.xMin, target.center.y);
                    if (target.x < origin.x)
                    {
                        start = new Vector3(origin.xMin, origin.center.y);
                        end = new Vector3(target.xMax, target.center.y);
                    }
                    float tangent = Mathf.Max(45f, Mathf.Abs(end.x - start.x) * .45f);
                    Vector3 startTangent = start + Vector3.right * Mathf.Sign(end.x - start.x) * tangent;
                    Vector3 endTangent = end - Vector3.right * Mathf.Sign(end.x - start.x) * tangent;
                    Handles.DrawBezier(start, end, startTangent, endTangent,
                        new Color(.45f, .75f, 1f, .8f), null, 2f);
                    GUI.Label(new Rect((start.x + end.x) * .5f - 60f, (start.y + end.y) * .5f - 10f, 120f, 20f),
                        transition.Label, EditorStyles.miniLabel);
                }
            }
        }

        void DrawNodes()
        {
            foreach (KeyValuePair<AbilityDefinition, Rect> pair in nodeRects)
            {
                AbilityDefinition ability = pair.Key;
                Rect rect = pair.Value;
                bool active = IsRuntimeActive(ability);
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = active ? new Color(.35f, 1f, .55f) : new Color(.72f, .78f, .88f);
                GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
                GUI.backgroundColor = previous;

                GUI.Label(new Rect(rect.x + 9f, rect.y + 7f, rect.width - 18f, 20f), ability.name,
                    EditorStyles.boldLabel);
                GUI.Label(new Rect(rect.x + 9f, rect.y + 29f, rect.width - 18f, 18f),
                    $"Priority {ability.Priority}  |  {ability.InterruptionPolicy}", EditorStyles.miniLabel);
                GUI.Label(new Rect(rect.x + 9f, rect.y + 48f, rect.width - 18f, 18f),
                    $"{ability.Transitions.Length} transition(s)  |  {ability.ExclusiveAuthority}",
                    EditorStyles.miniLabel);

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) Selection.activeObject = ability;
            }
        }

        bool IsRuntimeActive(AbilityDefinition ability)
        {
            if (!EditorApplication.isPlaying) return false;
            if (runtimeController == null)
            {
                CharacterAbilityController[] controllers =
                    FindObjectsByType<CharacterAbilityController>(FindObjectsInactive.Exclude);
                for (int i = 0; i < controllers.Length; i++)
                    if (controllers[i].AbilitySet == abilitySet)
                    {
                        runtimeController = controllers[i];
                        break;
                    }
            }
            if (runtimeController == null) return false;
            for (int i = 0; i < runtimeController.ActiveAbilities.Count; i++)
                if (runtimeController.ActiveAbilities[i].Definition == ability)
                    return true;
            return false;
        }
    }
}
