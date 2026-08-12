#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameSystems.Actions.Editor
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
            string title = label == GUIContent.none || string.IsNullOrEmpty(label.text) ? "Sequence" : label.text;
            GUIContent header = new($"{title} · {conditions.arraySize} conditions · {actions.arraySize} actions",
                "Conditions gate execution; actions then run sequentially.");
            Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, header, true);
            if (!property.isExpanded) return;

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
#endif
