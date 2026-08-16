#if UNITY_EDITOR
using GameSystems.Sequencing;
using GameSystems.Sequencing.Editor;
using GameSystems.Editor;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Abilities.Editor
{
    [CustomEditor(typeof(SequenceAbilityDefinition), true)]
    public sealed class SequenceAbilityDefinitionEditor : UnityEditor.Editor
    {
        bool showScheduling = true;
        bool showInterruptions = true;
        bool showConditions = true;
        bool showActions = true;
        bool showPresentation = true;

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SequenceAbilityDefinition ability = (SequenceAbilityDefinition)target;
            GameSystemsInspectorUI.Header(ability.name,
                $"{ability.Category} · P{ability.Priority} · {ability.Sequence.Conditions.Length} conditions · {ability.Sequence.Actions.Length} actions");

            if (GameSystemsInspectorUI.Foldout(ref showScheduling, "Scheduling", ability.AutoStart ? "Auto Start" : "Requested"))
            {
                Draw("autoStart"); Draw("priority"); Draw("cooldown");
                Draw("requiredAuthority"); Draw("exclusiveAuthority");
                Draw("refreshWhileActive");
                if (target.GetType() == typeof(SequenceAbilityDefinition)) Draw("category");
                GameSystemsInspectorUI.EndFoldout();
            }

            if (GameSystemsInspectorUI.Foldout(ref showInterruptions, "Interruptions", ability.InterruptionPolicy.ToString()))
            {
                Draw("interruptionPolicy"); Draw("transitions");
                GameSystemsInspectorUI.EndFoldout();
            }

            SerializedProperty sequence = serializedObject.FindProperty("sequence");
            if (GameSystemsInspectorUI.Foldout(ref showConditions, "Start Conditions",
                    ability.Sequence.Conditions.Length == 0 ? "always" : ability.Sequence.ConditionMode.ToString()))
            {
                EditorGUILayout.PropertyField(sequence.FindPropertyRelative("conditionMode"));
                ManagedReferenceListUtility.DrawLayout(sequence.FindPropertyRelative("conditions"), typeof(GameCondition));
                GameSystemsInspectorUI.EndFoldout();
            }

            if (GameSystemsInspectorUI.Foldout(ref showActions, "Execution",
                    ability.Sequence.Actions.Length == 0 ? "empty" : $"{ability.Sequence.Actions.Length} steps"))
            {
                Draw("completeWhenSequenceEnds");
                ManagedReferenceListUtility.DrawLayout(sequence.FindPropertyRelative("actions"), typeof(GameAction));
                GameSystemsInspectorUI.EndFoldout();
            }

            if (GameSystemsInspectorUI.Foldout(ref showPresentation, "Presentation"))
            {
                Draw("animationIntent"); Draw("startFeedback"); Draw("completeFeedback");
                GameSystemsInspectorUI.EndFoldout();
            }
            serializedObject.ApplyModifiedProperties();
        }

        void Draw(string name)
        {
            SerializedProperty property = serializedObject.FindProperty(name);
            if (property != null) EditorGUILayout.PropertyField(property, true);
        }
    }
}
#endif
