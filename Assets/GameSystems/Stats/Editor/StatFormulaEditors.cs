#if UNITY_EDITOR
using GameSystems.Editor;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Stats.Editor
{
    [CustomPropertyDrawer(typeof(StatFormula))]
    public sealed class StatFormulaDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight * 2f + 6f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty expression = property.FindPropertyRelative("expression");
            Rect labelRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect fieldRect = new(position.x, labelRect.yMax + 3f, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);
            expression.stringValue = EditorGUI.TextField(fieldRect, expression.stringValue);
        }
    }

    [CustomEditor(typeof(StatFormulaDefinition))]
    public sealed class StatFormulaDefinitionEditor : UnityEditor.Editor
    {
        CharacterStats previewStats;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            StatFormulaDefinition formula = (StatFormulaDefinition)target;
            GameSystemsInspectorUI.Header("Formula Preview", "Use [STAT_ID], for example [FOR] * 2 - [DEF].");
            previewStats = (CharacterStats)EditorGUILayout.ObjectField("Stats Source", previewStats,
                typeof(CharacterStats), true);

            if (previewStats == null)
            {
                EditorGUILayout.HelpBox("Assign a CharacterStats component to preview the formula.", MessageType.Info);
                return;
            }

            if (formula.TryEvaluate(previewStats, out float value, out string error))
                EditorGUILayout.HelpBox($"Result: {value:0.###}", MessageType.Info);
            else
                EditorGUILayout.HelpBox(error, MessageType.Warning);
        }
    }
}
#endif
