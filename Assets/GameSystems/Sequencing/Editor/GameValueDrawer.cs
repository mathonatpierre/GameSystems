#if UNITY_EDITOR
using System;
using System.Linq;
using GameSystems.Sequencing.Values;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Sequencing.Editor
{
    [CustomPropertyDrawer(typeof(GameValue), true)]
    public sealed class GameValueDrawer : PropertyDrawer
    {
        const float MenuWidth = 22f;

        public override float GetPropertyHeight(SerializedProperty property,
            GUIContent label) => ManagedReferenceDrawerUtility.GetHeight(property);

        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            Rect valueRect = position;
            valueRect.xMax -= MenuWidth + 2f;
            ManagedReferenceDrawerUtility.Draw(valueRect, property, label);
            Rect menuRect = new(valueRect.xMax + 2f, position.y, MenuWidth,
                EditorGUIUtility.singleLineHeight);
            if (GUI.Button(menuRect, EditorGUIUtility.IconContent("d_icon dropdown"),
                    EditorStyles.iconButton))
                ShowMenu(property);
        }

        static void ShowMenu(SerializedProperty property)
        {
            Type baseType = ResolveManagedReferenceType(property);
            GenericMenu menu = new();
            foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType)
                         .Where(value => !value.IsAbstract &&
                                         value.GetConstructor(Type.EmptyTypes) != null)
                         .OrderBy(value => value.Name))
            {
                Type selected = type;
                menu.AddItem(new GUIContent(MenuPath(selected)),
                    property.managedReferenceValue?.GetType() == selected,
                    () => Assign(property, selected));
            }
            menu.ShowAsContext();
        }

        static string MenuPath(Type type)
        {
            string domain = "Core";
            string[] segments = (type.Namespace ?? string.Empty).Split('.');
            if (segments.Length > 1 && segments[0] == "GameSystems" &&
                segments[1] != "Sequencing")
                domain = ObjectNames.NicifyVariableName(segments[1]);
            string name = type.Name;
            if (name.EndsWith("Value", StringComparison.Ordinal)) name = name[..^5];
            return $"{domain}/{ObjectNames.NicifyVariableName(name)}";
        }

        static Type ResolveManagedReferenceType(SerializedProperty property)
        {
            string declaration = property.managedReferenceFieldTypename;
            int separator = declaration.IndexOf(' ');
            if (separator <= 0) return typeof(GameValue);
            string assembly = declaration[..separator];
            string typeName = declaration[(separator + 1)..];
            return Type.GetType($"{typeName}, {assembly}") ?? typeof(GameValue);
        }

        static void Assign(SerializedProperty property, Type type)
        {
            property.serializedObject.Update();
            SerializedProperty current = property.serializedObject.FindProperty(property.propertyPath);
            if (current == null) return;
            current.managedReferenceValue = Activator.CreateInstance(type);
            current.isExpanded = true;
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
