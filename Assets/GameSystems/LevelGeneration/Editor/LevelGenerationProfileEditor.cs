#if UNITY_EDITOR
using GameSystems.Editor;
using UnityEditor;

namespace GameSystems.LevelGeneration.Editor
{
    [CustomEditor(typeof(LevelGenerationProfile))]
    public sealed class LevelGenerationProfileEditor : UnityEditor.Editor
    {
        bool showLayout = true;
        bool showWallJumps = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            LevelGenerationProfile profile = (LevelGenerationProfile)target;
            GameSystemsInspectorUI.Header(profile.name,
                $"{profile.PlatformTypes.Count} platform types · {profile.DefaultLength} length · {profile.DefaultDepth} depth");

            if (GameSystemsInspectorUI.Foldout(ref showLayout, "Layout"))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("layout"), true);
                GameSystemsInspectorUI.EndFoldout();
            }

            if (GameSystemsInspectorUI.Foldout(ref showWallJumps, "Wall Jumps"))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallJumps"), true);
                GameSystemsInspectorUI.EndFoldout();
            }

            GameSystemsInspectorUI.InlineScriptableObjectList(
                serializedObject.FindProperty("platformTypes"),
                typeof(PlatformTypeDefinition),
                "Platform Types");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
