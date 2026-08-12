#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameSystems.Actions.Editor
{
    [CustomPropertyDrawer(typeof(GameConditionGroup))]
    public sealed class GameConditionGroupDrawer : PropertyDrawer
    {
        const float Gap = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
            SerializedProperty conditions = property.FindPropertyRelative("conditions");
            return EditorGUIUtility.singleLineHeight * 2f + Gap * 2f +
                   ManagedReferenceListUtility.GetHeight(conditions, typeof(GameCondition));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty mode = property.FindPropertyRelative("mode");
            SerializedProperty conditions = property.FindPropertyRelative("conditions");
            string summary = $"{mode.enumDisplayNames[mode.enumValueIndex]} of {conditions.arraySize} conditions";
            Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, new GUIContent(summary,
                "Combines nested conditions into a reusable logical group."), true);
            if (!property.isExpanded) return;
            line.y += line.height + Gap;
            EditorGUI.PropertyField(line, mode);
            float listHeight = ManagedReferenceListUtility.GetHeight(conditions, typeof(GameCondition));
            ManagedReferenceListUtility.Draw(new Rect(position.x, line.yMax + Gap, position.width, listHeight),
                conditions, typeof(GameCondition));
        }
    }
}
#endif
