using GameSystems.Stats;
using GameSystems.Editor;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Stats.Editor
{
    [CustomEditor(typeof(CharacterStats))]
    public sealed class CharacterStatsEditor : UnityEditor.Editor
    {
        bool showAttributes = true;
        bool showStats = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty definition = serializedObject.FindProperty("definition");
            GameSystemsInspectorUI.InlineScriptableObject(definition,
                typeof(CharacterStatsDefinition), "Definition");
            serializedObject.ApplyModifiedProperties();

            CharacterStats characterStats = (CharacterStats)target;
            if (!Application.isPlaying)
            {
                GameSystemsInspectorUI.Header("Runtime Stats", "Values appear in Play Mode.");
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GameSystemsInspectorUI.Pill("PLAY MODE", new Color(.25f, .7f, .42f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Rebuild", GUILayout.Width(76f))) characterStats.Rebuild();
            }

            if (GameSystemsInspectorUI.Foldout(ref showAttributes, "Attributes"))
            {
                bool empty = true;
                foreach (RuntimeAttribute attribute in characterStats.RuntimeAttributes)
                {
                    empty = false;
                    Rect rect = EditorGUILayout.GetControlRect(false, 20f);
                    EditorGUI.ProgressBar(rect, attribute.Normalized,
                        $"{attribute.Definition.DisplayName}   {attribute.Current:0.##} / {attribute.Maximum:0.##}");
                }

                if (empty) EditorGUILayout.LabelField("None", GameSystemsInspectorUI.SmallMutedStyle);
                GameSystemsInspectorUI.EndFoldout();
            }

            if (GameSystemsInspectorUI.Foldout(ref showStats, "Stats"))
            {
                bool empty = true;
                foreach (RuntimeStat stat in characterStats.RuntimeStats)
                {
                    empty = false;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(stat.Definition.DisplayName, GUILayout.MinWidth(120f));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField($"{stat.Value:0.##}", EditorStyles.boldLabel, GUILayout.Width(58f));
                        EditorGUILayout.LabelField($"base {stat.BaseValue:0.##}", GameSystemsInspectorUI.SmallMutedStyle, GUILayout.Width(72f));
                        EditorGUILayout.LabelField($"mods {stat.ModifierCount}", GameSystemsInspectorUI.SmallMutedStyle, GUILayout.Width(58f));
                    }
                }

                if (empty) EditorGUILayout.LabelField("None", GameSystemsInspectorUI.SmallMutedStyle);
                GameSystemsInspectorUI.EndFoldout();
            }

            Repaint();
        }
    }
}
