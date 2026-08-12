#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Sequencing.Editor
{
    public static class ManagedReferenceDrawerUtility
    {
        public static float GetHeight(SerializedProperty property)
        {
            if (property.managedReferenceValue == null || !property.isExpanded) return EditorGUIUtility.singleLineHeight;
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            ForEachChild(property, child => height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing);
            return height;
        }

        public static void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawDebugBackground(position, property.managedReferenceValue);
            Rect header = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            string summary = property.managedReferenceValue switch
            {
                GameAction action => action.Summary,
                GameCondition condition => condition.Summary,
                null => "Missing reference",
                object value => ObjectNames.NicifyVariableName(value.GetType().Name)
            };
            GUIContent content = new(summary, BuildTooltip(property, summary));
            if (property.managedReferenceValue != null)
                property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, content, true);
            else
                EditorGUI.LabelField(header, content, EditorStyles.miniLabel);
            if (property.managedReferenceValue == null || !property.isExpanded) return;
            float y = header.yMax + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.indentLevel++;
            ForEachChild(property, child => { float height = EditorGUI.GetPropertyHeight(child, true); EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), child, true); y += height + EditorGUIUtility.standardVerticalSpacing; });
            EditorGUI.indentLevel--;
        }

        static void DrawDebugBackground(Rect position, object value)
        {
            if (!Application.isPlaying) return;
            Color color = value switch
            {
                GameAction { DebugStatus: GameActionDebugStatus.Running } => new Color(1f, .72f, .12f, .2f),
                GameAction { DebugStatus: GameActionDebugStatus.Succeeded } => new Color(.2f, .85f, .35f, .18f),
                GameAction { DebugStatus: GameActionDebugStatus.Failed } => new Color(1f, .2f, .2f, .2f),
                GameCondition { DebugStatus: GameConditionDebugStatus.Evaluating } => new Color(1f, .72f, .12f, .2f),
                GameCondition { DebugStatus: GameConditionDebugStatus.Succeeded } => new Color(.2f, .85f, .35f, .18f),
                GameCondition { DebugStatus: GameConditionDebugStatus.Failed } => new Color(1f, .2f, .2f, .2f),
                _ => Color.clear
            };
            if (color.a <= 0f) return;
            EditorGUI.DrawRect(new Rect(position.x - 2f, position.y, position.width + 4f, position.height), color);
        }

        static string BuildTooltip(SerializedProperty property, string summary)
        {
            if (property.managedReferenceValue == null) return summary;
            StringBuilder text = new();
            text.Append(ObjectNames.NicifyVariableName(property.managedReferenceValue.GetType().Name));
            text.Append('\n').Append(summary);
            ForEachChild(property, child =>
                text.Append('\n').Append(child.displayName).Append(": ").Append(FormatValue(child)));
            return text.ToString();
        }

        static string FormatValue(SerializedProperty property) => property.propertyType switch
        {
            SerializedPropertyType.Boolean => property.boolValue.ToString().ToLowerInvariant(),
            SerializedPropertyType.Integer => property.intValue.ToString(),
            SerializedPropertyType.Float => property.floatValue.ToString("0.###"),
            SerializedPropertyType.String => string.IsNullOrEmpty(property.stringValue) ? "(empty)" : property.stringValue,
            SerializedPropertyType.Enum => property.enumValueIndex >= 0 && property.enumValueIndex < property.enumDisplayNames.Length
                ? property.enumDisplayNames[property.enumValueIndex] : property.enumValueIndex.ToString(),
            SerializedPropertyType.ObjectReference => property.objectReferenceValue != null ? property.objectReferenceValue.name : "None",
            SerializedPropertyType.ManagedReference => property.managedReferenceValue?.GetType().Name ?? "None",
            _ when property.isArray => $"{property.arraySize} items",
            _ => property.displayName
        };

        static void ForEachChild(SerializedProperty property, Action<SerializedProperty> action)
        {
            SerializedProperty iterator = property.Copy(); SerializedProperty end = iterator.GetEndProperty(); bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end)) { enterChildren = false; action(iterator.Copy()); }
        }
    }
}
#endif
