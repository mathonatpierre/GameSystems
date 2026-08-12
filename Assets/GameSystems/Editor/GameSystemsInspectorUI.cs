#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Editor
{
    public static class GameSystemsInspectorUI
    {
        static GUIStyle titleStyle;
        static GUIStyle pillStyle;
        static GUIStyle smallMutedStyle;
        static readonly Dictionary<string, bool> InlineFoldouts = new();

        public static GUIStyle TitleStyle => titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft
        };

        public static GUIStyle PillStyle => pillStyle ??= new GUIStyle(EditorStyles.miniButton)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10,
            fixedHeight = 18f,
            padding = new RectOffset(7, 7, 1, 1)
        };

        public static GUIStyle SmallMutedStyle => smallMutedStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = false,
            normal = { textColor = EditorGUIUtility.isProSkin ? new Color(.68f, .68f, .68f) : new Color(.36f, .36f, .36f) }
        };

        public static void Header(string title, string subtitle = null)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, TitleStyle);
                if (!string.IsNullOrWhiteSpace(subtitle))
                    EditorGUILayout.LabelField(subtitle, SmallMutedStyle);
            }
        }

        public static bool Foldout(ref bool expanded, string title, string summary = null)
        {
            Rect rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Rect row = EditorGUILayout.GetControlRect(false, 20f);
            expanded = EditorGUI.Foldout(row, expanded, title, true, EditorStyles.foldoutHeader);
            if (!string.IsNullOrWhiteSpace(summary))
            {
                Rect summaryRect = new(row.x + 130f, row.y + 1f, row.width - 135f, row.height);
                EditorGUI.LabelField(summaryRect, summary, SmallMutedStyle);
            }

            if (!expanded) EditorGUILayout.EndVertical();
            return expanded;
        }

        public static void EndFoldout()
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.EndVertical();
        }

        public static void Pill(string text, Color color)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Label(text, PillStyle, GUILayout.Width(Mathf.Clamp(text.Length * 7f + 20f, 58f, 160f)));
            GUI.backgroundColor = previous;
        }

        public static void ThinSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, .08f) : new Color(0f, 0f, 0f, .12f));
        }

        public static void InlineScriptableObject(SerializedProperty property, Type type,
            string label = null, bool expandedByDefault = false)
        {
            if (property == null) return;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property, new GUIContent(label ?? property.displayName));
                    UnityEngine.Object reference = property.objectReferenceValue;
                    using (new EditorGUI.DisabledScope(reference == null))
                    {
                        if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(42f)))
                            EditorGUIUtility.PingObject(reference);
                    }
                }

                UnityEngine.Object target = property.objectReferenceValue;
                if (target == null || target is not ScriptableObject)
                    return;
                if (type != null && !type.IsInstanceOfType(target))
                    return;

                string key = GlobalObjectId.GetGlobalObjectIdSlow(property.serializedObject.targetObject) + ":" + property.propertyPath;
                if (!InlineFoldouts.ContainsKey(key)) InlineFoldouts[key] = expandedByDefault;
                InlineFoldouts[key] = EditorGUILayout.Foldout(InlineFoldouts[key],
                    $"Edit {target.name}", true);
                if (!InlineFoldouts[key]) return;

                EditorGUI.indentLevel++;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    SerializedObject nested = new(target);
                    nested.Update();
                    DrawNestedProperties(nested);
                    nested.ApplyModifiedProperties();
                }
                EditorGUI.indentLevel--;
            }
        }

        public static void InlineScriptableObjectList(SerializedProperty array,
            Type elementType, string title)
        {
            if (array == null) return;
            if (!array.isArray)
            {
                EditorGUILayout.PropertyField(array, true);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(array.FindPropertyRelative("Array.size"),
                    new GUIContent(title + " Size"));
                for (int i = 0; i < array.arraySize; i++)
                    InlineScriptableObject(array.GetArrayElementAtIndex(i), elementType,
                        $"{i + 1}", false);
            }
        }

        static void DrawNestedProperties(SerializedObject nested)
        {
            SerializedProperty iterator = nested.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script") continue;
                EditorGUILayout.PropertyField(iterator, true);
            }
        }
    }
}
#endif
