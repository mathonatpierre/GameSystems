#if UNITY_EDITOR
using GameSystems.Actions;
using GameSystems.Actions.Editor;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Abilities.Editor
{
    [CustomPropertyDrawer(typeof(CharacterAIDecision))]
    public sealed class CharacterAIDecisionDrawer : PropertyDrawer
    {
        const float Gap = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
            float fields = EditorGUIUtility.singleLineHeight * 5f + Gap * 6f;
            return fields + ManagedReferenceListUtility.GetHeight(
                property.FindPropertyRelative("conditions"), typeof(GameCondition));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty name = property.FindPropertyRelative("label");
            SerializedProperty ability = property.FindPropertyRelative("ability");
            SerializedProperty priority = property.FindPropertyRelative("priority");
            string abilityName = ability.objectReferenceValue != null ? ability.objectReferenceValue.name : "No ability";
            GUIContent header = new($"{name.stringValue} · {abilityName} · P{priority.intValue}",
                "AI decision. Conditions are evaluated before requesting its ability.");
            Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, header, true);
            if (!property.isExpanded) return;
            float y = line.yMax + Gap;
            Draw(ref y, position, property.FindPropertyRelative("label"));
            Draw(ref y, position, ability);
            Draw(ref y, position, priority);
            Draw(ref y, position, property.FindPropertyRelative("minimumInterval"));
            Draw(ref y, position, property.FindPropertyRelative("conditionMode"));
            SerializedProperty conditions = property.FindPropertyRelative("conditions");
            float height = ManagedReferenceListUtility.GetHeight(conditions, typeof(GameCondition));
            ManagedReferenceListUtility.Draw(new Rect(position.x, y, position.width, height),
                conditions, typeof(GameCondition));
        }

        static void Draw(ref float y, Rect position, SerializedProperty property)
        {
            Rect field = new(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(field, property);
            y = field.yMax + Gap;
        }
    }
}
#endif
