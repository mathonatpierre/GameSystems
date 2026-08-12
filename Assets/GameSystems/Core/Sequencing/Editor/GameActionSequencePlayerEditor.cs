#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameSystems.Actions.Editor
{
    [CustomEditor(typeof(GameActionSequencePlayer))]
    public sealed class GameActionSequencePlayerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sequenceAsset"));
            if (serializedObject.FindProperty("sequenceAsset").objectReferenceValue == null)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("inlineSequence"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnEnable"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onCompleted"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onRejected"));
            serializedObject.ApplyModifiedProperties();

            GameActionSequencePlayer player = (GameActionSequencePlayer)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play")) player.Play();
                if (GUILayout.Button("Stop")) player.Stop();
            }
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("State", player.State.ToString());
                Repaint();
            }
        }
    }
}
#endif
