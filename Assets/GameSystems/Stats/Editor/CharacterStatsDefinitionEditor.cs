#if UNITY_EDITOR
using GameSystems.Editor;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Stats.Editor
{
    [CustomEditor(typeof(CharacterStatsDefinition))]
    public sealed class CharacterStatsDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GameSystemsInspectorUI.InlineScriptableObjectList(
                serializedObject.FindProperty("stats"),
                typeof(StatDefinition),
                "Stats");
            GameSystemsInspectorUI.InlineScriptableObjectList(
                serializedObject.FindProperty("attributes"),
                typeof(AttributeDefinition),
                "Attributes");
            GameSystemsInspectorUI.InlineScriptableObject(
                serializedObject.FindProperty("primaryHealth"),
                typeof(AttributeDefinition),
                "Primary Health");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
