using GameSystems.Sequencing;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Feedbacks.Editor
{
    [CustomEditor(typeof(FeedbackSequence))]
    public sealed class FeedbackSequenceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("concurrency"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumInstances"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("channel"));
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("actionSequence"),
                new GUIContent("Sequence"), true);
            serializedObject.ApplyModifiedProperties();

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
                if (GUILayout.Button("Preview"))
                {
                    GameObject source = Selection.activeGameObject;
                    var feedback = new FeedbackRuntimeContext
                    {
                        Position = source != null ? source.transform.position : Vector3.zero,
                        Rotation = source != null ? source.transform.rotation : Quaternion.identity
                    };
                    FeedbackService.Play((FeedbackSequence)target,
                        new GameActionContext(source, source, null, feedback));
                }
        }
    }
}
