#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using GameSystems.Sequencing.Values;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Sequencing.Editor
{
    [CustomPropertyDrawer(typeof(ComponentTarget), true)]
    public sealed class ComponentTargetDrawer : PropertyDrawer
    {
        const float Gap = 3f;
        const float SourceWidth = 105f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty source = property.FindPropertyRelative("source");
            return EditorGUIUtility.singleLineHeight +
                   (HasConfiguration(source)
                       ? EditorGUIUtility.standardVerticalSpacing + SourceBoxHeight(source)
                       : 0f);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty explicitTarget = property.FindPropertyRelative("explicitTarget");
            SerializedProperty useExplicit = property.FindPropertyRelative("useExplicitTarget");
            SerializedProperty source = property.FindPropertyRelative("source");
            SerializedProperty scope = property.FindPropertyRelative("searchScope");
            Rect line = new(position.x, position.y, position.width,
                EditorGUIUtility.singleLineHeight);
            line = EditorGUI.PrefixLabel(line, label);

            bool direct = useExplicit.boolValue || explicitTarget.objectReferenceValue != null;
            Rect sourceRect = new(line.x, line.y, Mathf.Min(SourceWidth, line.width * .4f), line.height);
            Rect scopeRect = new(sourceRect.xMax + Gap, line.y,
                Mathf.Min(82f, line.width * .3f), line.height);
            Rect valueRect = new(scopeRect.xMax + Gap, line.y,
                Mathf.Max(0f, line.xMax - scopeRect.xMax - Gap), line.height);

            if (GUI.Button(sourceRect, direct ? "Reference" : ShortSummary(source), EditorStyles.popup))
                ShowSourceMenu(property, explicitTarget, source);

            using (new EditorGUI.DisabledScope(direct))
                EditorGUI.PropertyField(scopeRect, scope, GUIContent.none);

            if (direct)
                EditorGUI.PropertyField(valueRect, explicitTarget, GUIContent.none);
            else EditorGUI.LabelField(valueRect, "Resolved", EditorStyles.miniLabel);

            if (!direct && HasConfiguration(source))
            {
                Rect details = new(position.x, line.yMax + EditorGUIUtility.standardVerticalSpacing,
                    position.width, SourceBoxHeight(source));
                DrawSourceBox(details, source);
            }
        }

        static string ShortSummary(SerializedProperty source)
        {
            if (source?.managedReferenceValue is GameValue value)
                return value.Summary;
            return "Select Source";
        }

        static bool HasConfiguration(SerializedProperty source)
        {
            return ConfigurationFields(source).Length > 0;
        }

        static FieldInfo[] ConfigurationFields(SerializedProperty source) =>
            source?.managedReferenceValue?.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => !field.IsStatic &&
                    (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null ||
                     field.GetCustomAttribute<SerializeReference>() != null))
                .ToArray() ?? Array.Empty<FieldInfo>();

        static float SourceBoxHeight(SerializedProperty source)
        {
            float height = EditorGUIUtility.singleLineHeight + 8f;
            foreach (FieldInfo field in ConfigurationFields(source))
            {
                SerializedProperty child = source.FindPropertyRelative(field.Name);
                if (child != null) height += EditorGUI.GetPropertyHeight(child, true) + Gap;
            }
            return height;
        }

        static void DrawSourceBox(Rect rect, SerializedProperty source)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            Rect line = new(rect.x + 7f, rect.y + 4f, rect.width - 14f,
                EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(line, $"{ShortSummary(source)} Binding", EditorStyles.boldLabel);
            line.y += line.height + Gap;
            foreach (FieldInfo field in ConfigurationFields(source))
            {
                SerializedProperty child = source.FindPropertyRelative(field.Name);
                if (child == null) continue;
                line.height = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(line, child, true);
                line.y += line.height + Gap;
            }
        }

        static void ShowSourceMenu(SerializedProperty target, SerializedProperty explicitTarget,
            SerializedProperty source)
        {
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Reference"), explicitTarget.objectReferenceValue != null,
                () => SetReferenceMode(target));
            menu.AddSeparator(string.Empty);
            foreach (Type type in TypeCache.GetTypesDerivedFrom<GameObjectValue>()
                         .Where(value => !value.IsAbstract &&
                                         value.GetConstructor(Type.EmptyTypes) != null)
                         .OrderBy(MenuPath))
            {
                Type selected = type;
                menu.AddItem(new GUIContent(MenuPath(type)),
                    explicitTarget.objectReferenceValue == null &&
                    source.managedReferenceValue?.GetType() == type,
                    () => SetSource(target, selected));
            }
            menu.ShowAsContext();
        }

        static string MenuPath(Type type)
        {
            string name = ObjectNames.NicifyVariableName(type.Name
                .Replace("GameObjectValue", string.Empty));
            string[] segments = (type.Namespace ?? string.Empty).Split('.');
            return segments.Length > 1 && segments[1] != "Sequencing"
                ? $"{ObjectNames.NicifyVariableName(segments[1])}/{name}"
                : name;
        }

        static void SetReferenceMode(SerializedProperty property)
        {
            property.serializedObject.Update();
            SerializedProperty current = property.serializedObject.FindProperty(property.propertyPath);
            current.FindPropertyRelative("useExplicitTarget").boolValue = true;
            current.FindPropertyRelative("source").managedReferenceValue = null;
            property.serializedObject.ApplyModifiedProperties();
        }

        static void SetSource(SerializedProperty property, Type type)
        {
            property.serializedObject.Update();
            SerializedProperty current = property.serializedObject.FindProperty(property.propertyPath);
            current.FindPropertyRelative("useExplicitTarget").boolValue = false;
            current.FindPropertyRelative("explicitTarget").objectReferenceValue = null;
            SerializedProperty source = current.FindPropertyRelative("source");
            source.managedReferenceValue = Activator.CreateInstance(type);
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
