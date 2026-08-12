#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GameSystems.Actions.Editor
{
    public static class ManagedReferenceListUtility
    {
        public static void DrawLayout(SerializedProperty property, Type baseType)
        {
            Create(property, baseType).DoLayoutList();
        }

        public static float GetHeight(SerializedProperty property, Type baseType) =>
            Create(property, baseType).GetHeight();

        public static void Draw(Rect rect, SerializedProperty property, Type baseType) =>
            Create(property, baseType).DoList(rect);

        static ReorderableList Create(SerializedProperty property, Type baseType) =>
            new(property.serializedObject, property, true, false, true, true)
            {
                elementHeightCallback = index => ElementHeight(property, index),
                drawElementCallback = (rect, index, active, focused) => DrawElement(rect, property, index),
                onAddDropdownCallback = (rect, _) => ShowAddMenu(property, baseType),
                onRemoveCallback = value => Remove(value.serializedProperty, value.index)
            };

        static float ElementHeight(SerializedProperty array, int index)
        {
            if (index < 0 || index >= array.arraySize) return EditorGUIUtility.singleLineHeight + 4f;
            return EditorGUI.GetPropertyHeight(array.GetArrayElementAtIndex(index), true) + 4f;
        }

        static void DrawElement(Rect rect, SerializedProperty array, int index)
        {
            if (index < 0 || index >= array.arraySize) return;
            rect.y += 2f;
            rect.height = EditorGUI.GetPropertyHeight(array.GetArrayElementAtIndex(index), true);
            EditorGUI.PropertyField(rect, array.GetArrayElementAtIndex(index), GUIContent.none, true);
        }

        static void ShowAddMenu(SerializedProperty array, Type baseType)
        {
            SerializedObject owner = array.serializedObject;
            string path = array.propertyPath;
            GenericMenu menu = new();
            foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType)
                         .Where(type => !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                         .OrderBy(MenuPath))
            {
                Type selected = type;
                menu.AddItem(new GUIContent(MenuPath(type)), false, () => Add(owner, path, selected));
            }
            if (menu.GetItemCount() == 0) menu.AddDisabledItem(new GUIContent("No compatible types"));
            menu.ShowAsContext();
        }

        static string MenuPath(Type type)
        {
            string kind = typeof(GameAction).IsAssignableFrom(type) ? "Actions" : "Conditions";
            string domain = "Core";
            string[] segments = (type.Namespace ?? string.Empty).Split('.');
            if (segments.Length > 1 && segments[0] == "GameSystems" && segments[1] != "Actions")
                domain = ObjectNames.NicifyVariableName(segments[1]);
            string name = type.Name;
            if (name.EndsWith("Action", StringComparison.Ordinal)) name = name[..^6];
            else if (name.EndsWith("Condition", StringComparison.Ordinal)) name = name[..^9];
            return $"{kind}/{domain}/{ObjectNames.NicifyVariableName(name)}";
        }

        static void Add(SerializedObject owner, string path, Type type)
        {
            owner.Update();
            SerializedProperty array = owner.FindProperty(path);
            if (array == null) return;
            int index = array.arraySize++;
            SerializedProperty element = array.GetArrayElementAtIndex(index);
            element.managedReferenceValue = Activator.CreateInstance(type);
            element.isExpanded = false;
            owner.ApplyModifiedProperties();
        }

        static void Remove(SerializedProperty array, int index)
        {
            if (index < 0 || index >= array.arraySize) return;
            array.DeleteArrayElementAtIndex(index);
            array.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
