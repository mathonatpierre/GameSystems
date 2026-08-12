#if UNITY_EDITOR
using GameSystems.Sequencing;
using GameSystems.Sequencing.Editor;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Abilities.Editor
{
    [CustomPropertyDrawer(typeof(AbilityTransitionDefinition))]
    public sealed class AbilityTransitionDefinitionDrawer : PropertyDrawer
    {
        const float Gap = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
            float height = EditorGUIUtility.singleLineHeight + Gap;
            height += FieldHeight(property, "label") + FieldHeight(property, "trigger") +
                      FieldHeight(property, "target") + FieldHeight(property, "priority") +
                      FieldHeight(property, "completeSource");
            SerializedProperty sequence = property.FindPropertyRelative("sequence");
            height += FieldHeight(sequence, "conditionMode");
            height += ManagedReferenceListUtility.GetHeight(sequence.FindPropertyRelative("conditions"), typeof(GameCondition));
            height += ManagedReferenceListUtility.GetHeight(sequence.FindPropertyRelative("actions"), typeof(GameAction));
            return height + Gap * 8f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty name = property.FindPropertyRelative("label");
            SerializedProperty trigger = property.FindPropertyRelative("trigger");
            SerializedProperty target = property.FindPropertyRelative("target");
            string destination = target.objectReferenceValue != null ? target.objectReferenceValue.name : "Complete";
            GUIContent header = new($"{name.stringValue} · {trigger.enumDisplayNames[trigger.enumValueIndex]} -> {destination}",
                "Conditional transition. Expand to edit its conditions and actions inline.");
            Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, header, true);
            if (!property.isExpanded) return;
            float y = line.yMax + Gap;
            DrawField(ref y, position, property, "label");
            DrawField(ref y, position, property, "trigger");
            DrawField(ref y, position, property, "target");
            DrawField(ref y, position, property, "priority");
            DrawField(ref y, position, property, "completeSource");
            SerializedProperty sequence = property.FindPropertyRelative("sequence");
            DrawField(ref y, position, sequence, "conditionMode");
            SerializedProperty conditions = sequence.FindPropertyRelative("conditions");
            float conditionsHeight = ManagedReferenceListUtility.GetHeight(conditions, typeof(GameCondition));
            ManagedReferenceListUtility.Draw(new Rect(position.x, y, position.width, conditionsHeight), conditions, typeof(GameCondition));
            y += conditionsHeight + Gap;
            SerializedProperty actions = sequence.FindPropertyRelative("actions");
            float actionsHeight = ManagedReferenceListUtility.GetHeight(actions, typeof(GameAction));
            ManagedReferenceListUtility.Draw(new Rect(position.x, y, position.width, actionsHeight), actions, typeof(GameAction));
        }

        static float FieldHeight(SerializedProperty root, string name) =>
            EditorGUI.GetPropertyHeight(root.FindPropertyRelative(name), true);

        static void DrawField(ref float y, Rect position, SerializedProperty root, string name)
        {
            SerializedProperty child = root.FindPropertyRelative(name);
            float height = EditorGUI.GetPropertyHeight(child, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), child, true);
            y += height + Gap;
        }
    }
}
#endif
