#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using GameSystems.Sequencing.Values;

namespace GameSystems.Sequencing.Editor
{
    public static class ManagedReferenceDrawerUtility
    {
        static GUIStyle richFoldout;
        public static float GetHeight(SerializedProperty property)
        {
            float diagnosticHeight = GetDiagnosticHeight(property.managedReferenceValue, property.serializedObject.targetObject);
            if (property.managedReferenceValue == null || !property.isExpanded)
                return EditorGUIUtility.singleLineHeight + diagnosticHeight;
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + diagnosticHeight;
            ForEachChild(property, child => { if (child.name != "disabled" && child.name != "negated") height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing; });
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
                GameValue gameValue => gameValue.Summary,
                null => "Missing reference",
                object value => ObjectNames.NicifyVariableName(value.GetType().Name)
            };
            GUIContent content = new(HighlightVariables(summary), BuildTooltip(property, summary));
            SerializedProperty disabled = property.FindPropertyRelative("disabled");
            SerializedProperty negated = property.FindPropertyRelative("negated");
            Rect toggleRect = new(header.x, header.y, 18f, header.height);
            header.xMin += disabled != null ? 24f : 0f;
            Rect negateRect = new(header.xMax - 18f, header.y, 18f, header.height);
            if (negated != null) header.xMax -= 20f;
            if (disabled != null)
            {
                bool enabled = !disabled.boolValue;
                bool next = EditorGUI.Toggle(toggleRect,
                    new GUIContent("", "Enable or disable this line."), enabled);
                if (next != enabled) disabled.boolValue = !next;
            }
            if (negated != null)
                negated.boolValue = EditorGUI.Toggle(negateRect,
                    new GUIContent("!", "Invert this condition."), negated.boolValue);
            using (new EditorGUI.DisabledScope(disabled?.boolValue == true))
            {
            if (property.managedReferenceValue != null)
                property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, content,
                    true, RichFoldout);
            else
                EditorGUI.LabelField(header, content, EditorStyles.miniLabel);
            }
            float y = DrawDiagnostic(position, header.yMax, property.managedReferenceValue,
                property.serializedObject.targetObject);
            if (property.managedReferenceValue == null || !property.isExpanded) return;
            y += EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(disabled?.boolValue == true))
                ForEachChild(property, child => { if (child.name is "disabled" or "negated") return; float height = EditorGUI.GetPropertyHeight(child, true); EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), child, true); y += height + EditorGUIUtility.standardVerticalSpacing; });
            EditorGUI.indentLevel--;
        }

        static GUIStyle RichFoldout => richFoldout ??= new GUIStyle(EditorStyles.foldout)
        { richText = true };

        static string HighlightVariables(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            StringBuilder result = new();
            int cursor = 0;
            while (cursor < value.Length)
            {
                int open = value.IndexOf('[', cursor);
                int close = open >= 0 ? value.IndexOf(']', open + 1) : -1;
                if (open < 0 || close < 0) { result.Append(value, cursor, value.Length - cursor); break; }
                result.Append(value, cursor, open - cursor);
                result.Append("<color=#E8C85A>").Append(value, open, close - open + 1)
                    .Append("</color>");
                cursor = close + 1;
            }
            return result.ToString();
        }

        static float GetDiagnosticHeight(object value, UnityEngine.Object inspectedObject)
        {
            string message = GetDiagnostic(value);
            if (!Application.isPlaying || string.IsNullOrWhiteSpace(message)) return 0f;
            float width = Mathf.Max(100f, EditorGUIUtility.currentViewWidth - 70f);
            return EditorStyles.helpBox.CalcHeight(new GUIContent(message), width) +
                   EditorGUIUtility.standardVerticalSpacing;
        }

        static float DrawDiagnostic(Rect position, float y, object value,
            UnityEngine.Object inspectedObject)
        {
            float height = GetDiagnosticHeight(value, inspectedObject);
            if (height <= 0f) return y;
            string message = GetDiagnostic(value);
            MessageType type = value is GameAction { DebugStatus: GameActionDebugStatus.Failed } ||
                               value is GameCondition { DebugStatus: GameConditionDebugStatus.Failed }
                ? MessageType.Error : MessageType.Warning;
            EditorGUI.HelpBox(new Rect(position.x, y + EditorGUIUtility.standardVerticalSpacing,
                position.width, height - EditorGUIUtility.standardVerticalSpacing), message, type);
            return y + height;
        }

        static string GetDiagnostic(object value) => value switch
        {
            GameAction action => action.DebugMessage,
            GameCondition condition => condition.DebugMessage,
            _ => null
        };

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
