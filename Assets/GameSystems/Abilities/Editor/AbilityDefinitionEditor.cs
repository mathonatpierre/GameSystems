#if UNITY_EDITOR
using GameSystems.Editor;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Abilities.Editor
{
    [CustomEditor(typeof(AbilityDefinition), true)]
    public class AbilityDefinitionEditor : UnityEditor.Editor
    {
        bool showScheduling = true;
        bool showInterruptions = true;
        bool showTransitions = true;
        bool showPresentation = true;
        bool showSpecific = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AbilityDefinition ability = (AbilityDefinition)target;
            DrawHeader(ability);
            DrawScheduling();
            DrawInterruptions();
            DrawTransitions();
            DrawPresentation();
            DrawSpecificProperties();
            serializedObject.ApplyModifiedProperties();
        }

        protected static void DrawHeader(AbilityDefinition ability)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GameSystemsInspectorUI.Pill(ability.Category.ToString(), AbilityEditorStyles.CategoryColor(ability.Category));
                EditorGUILayout.LabelField(ability.name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(ability.ActivationPolicy.ToString(), GameSystemsInspectorUI.SmallMutedStyle, GUILayout.Width(76f));
                EditorGUILayout.LabelField($"P {ability.Priority}", GameSystemsInspectorUI.SmallMutedStyle, GUILayout.Width(36f));
            }
        }

        void DrawScheduling()
        {
            if (!GameSystemsInspectorUI.Foldout(ref showScheduling, "Scheduling")) return;
            Draw("activationPolicy");
            Draw("priority");
            Draw("cooldown");
            Draw("requiredAuthority");
            Draw("exclusiveAuthority");
            GameSystemsInspectorUI.EndFoldout();
        }

        void DrawInterruptions()
        {
            if (!GameSystemsInspectorUI.Foldout(ref showInterruptions, "Interruptions")) return;
            Draw("interruptionPolicy");
            GameSystemsInspectorUI.EndFoldout();
        }

        void DrawTransitions()
        {
            if (!GameSystemsInspectorUI.Foldout(ref showTransitions, "Transitions")) return;
            Draw("transitions");
            GameSystemsInspectorUI.EndFoldout();
        }

        void DrawPresentation()
        {
            if (!GameSystemsInspectorUI.Foldout(ref showPresentation, "Presentation")) return;
            Draw("animationIntent");
            GameSystemsInspectorUI.InlineScriptableObject(
                serializedObject.FindProperty("startFeedback"),
                typeof(GameSystems.Feedbacks.FeedbackSequence),
                "Start Feedback");
            GameSystemsInspectorUI.InlineScriptableObject(
                serializedObject.FindProperty("completeFeedback"),
                typeof(GameSystems.Feedbacks.FeedbackSequence),
                "Complete Feedback");
            GameSystemsInspectorUI.EndFoldout();
        }

        void DrawSpecificProperties()
        {
            if (!GameSystemsInspectorUI.Foldout(ref showSpecific, "Specific")) return;
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (IsBaseProperty(iterator.propertyPath)) continue;
                EditorGUILayout.PropertyField(iterator, true);
            }
            GameSystemsInspectorUI.EndFoldout();
        }

        void Draw(string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null) EditorGUILayout.PropertyField(property, true);
        }

        static bool IsBaseProperty(string propertyPath)
        {
            return propertyPath is "m_Script" or
                "activationPolicy" or
                "priority" or
                "cooldown" or
                "requiredAuthority" or
                "exclusiveAuthority" or
                "interruptionPolicy" or
                "transitions" or
                "animationIntent" or
                "startFeedback" or
                "completeFeedback";
        }
    }
}
#endif
