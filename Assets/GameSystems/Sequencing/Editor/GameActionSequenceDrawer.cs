#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameSystems.Sequencing.Editor
{
    [CustomPropertyDrawer(typeof(GameActionSequence))]
    public sealed class GameActionSequenceDrawer : PropertyDrawer
    {
        const float Gap = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
            SerializedProperty conditions = property.FindPropertyRelative("conditions");
            SerializedProperty actions = property.FindPropertyRelative("actions");
            return EditorGUIUtility.singleLineHeight * 2f + Gap * 3f +
                   ManagedReferenceListUtility.GetHeight(conditions, typeof(GameCondition)) +
                   ManagedReferenceListUtility.GetHeight(actions, typeof(GameAction));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty mode = property.FindPropertyRelative("conditionMode");
            SerializedProperty conditions = property.FindPropertyRelative("conditions");
            SerializedProperty actions = property.FindPropertyRelative("actions");
            SerializedProperty disabled = property.FindPropertyRelative("disabled");
            string title = label == GUIContent.none || string.IsNullOrEmpty(label.text) ? "Sequence" : label.text;
            GUIContent header = new($"{title} · {conditions.arraySize} conditions · {actions.arraySize} actions",
                "Conditions gate execution; actions then run sequentially.");
            Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect toggle = new(line.xMax - 18f, line.y, 18f, line.height);
            line.xMax -= 20f;
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, header, true);
            bool enabled = !disabled.boolValue;
            bool next = EditorGUI.Toggle(toggle, new GUIContent("", "Enable or disable this sequence."), enabled);
            if (next != enabled) disabled.boolValue = !next;
            if (!property.isExpanded) return;

            using (new EditorGUI.DisabledScope(disabled.boolValue))
            {
            line.y += line.height + Gap;
            EditorGUI.PropertyField(line, mode, new GUIContent("Condition Mode"));
            float y = line.yMax + Gap;
            float conditionHeight = ManagedReferenceListUtility.GetHeight(conditions, typeof(GameCondition));
            ManagedReferenceListUtility.Draw(new Rect(position.x, y, position.width, conditionHeight),
                conditions, typeof(GameCondition));
            y += conditionHeight + Gap;
            float actionHeight = ManagedReferenceListUtility.GetHeight(actions, typeof(GameAction));
            ManagedReferenceListUtility.Draw(new Rect(position.x, y, position.width, actionHeight),
                actions, typeof(GameAction));
            }
        }
    }
}
#endif
