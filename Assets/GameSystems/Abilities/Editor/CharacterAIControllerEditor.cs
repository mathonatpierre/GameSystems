#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

using GameSystems.Characters;

namespace GameSystems.Abilities.Editor
{
    [CustomEditor(typeof(CharacterAIController))]
    public sealed class CharacterAIControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying) return;
            CharacterAIController controller = (CharacterAIController)target;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Target", controller.CurrentTarget, typeof(Transform), true);
            EditorGUILayout.LabelField("Behavior", controller.CurrentBehavior ?? "None");
            EditorGUILayout.LabelField("Distance", controller.LastContext.Distance.ToString("0.00"));
            EditorGUILayout.Toggle("Line Of Sight", controller.LastContext.HasLineOfSight);
            Repaint();
        }
    }
}
#endif
