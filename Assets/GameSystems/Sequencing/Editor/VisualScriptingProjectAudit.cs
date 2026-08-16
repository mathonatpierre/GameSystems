#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameSystems.Sequencing.Editor
{
    public static class VisualScriptingProjectAudit
    {
        static readonly string[] LegacyTypes =
        {
            "MountRideableAction", "DismountRideableAction", "ReplaceCharacterAction",
            "MoveCharacterAlongArcAction", "MoveAwayFromContactArcAction", "StompAction",
            "AddContactResourceAction", "ClearCharacterResourceAction",
            "PrepareRiderDismountAction", "FollowRiderTransitionAnimationAction",
            "CharacterContactCondition", "AirTimeCondition", "FallDistanceCondition",
            "VelocityCondition", "RequestValueCondition", "StatThresholdCondition",
            "AttributeThresholdCondition", "AITargetDistanceCondition"
        };

        [MenuItem("Game Systems/Sequencing/Audit Entire Project Migration")]
        public static void Run()
        {
            string[] paths = AssetDatabase.GetAllAssetPaths();
            int scenes = 0, prefabs = 0, dataAssets = 0, objects = 0;
            foreach (string path in paths)
            {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal)) continue;
                CheckLegacyText(path);
                if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    foreach (GameObject root in scene.GetRootGameObjects()) objects += CheckHierarchy(root, path);
                    scenes++;
                }
                else if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    GameObject root = PrefabUtility.LoadPrefabContents(path);
                    try { objects += CheckHierarchy(root, path); }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                    prefabs++;
                }
                else if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                        if (asset != null) { CheckManagedReferences(asset, path); objects++; }
                    dataAssets++;
                }
            }
            Debug.Log($"[Visual Scripting Project Audit] PASS · {scenes} scenes · " +
                      $"{prefabs} prefabs · {dataAssets} assets · {objects} objects checked");
        }

        static int CheckHierarchy(GameObject root, string path)
        {
            int count = 0;
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                count++;
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject);
                if (missing > 0)
                    throw new InvalidOperationException($"{path}: {item.name} has {missing} missing scripts.");
                foreach (Component component in item.GetComponents<Component>())
                    if (component != null) CheckManagedReferences(component, path);
            }
            return count;
        }

        static void CheckManagedReferences(UnityEngine.Object target, string path)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.GetIterator();
            bool enterChildren = true;
            while (property.Next(enterChildren))
            {
                enterChildren = true;
                if (property.propertyType != SerializedPropertyType.ManagedReference) continue;
                if (property.managedReferenceId > 0 && property.managedReferenceValue == null)
                    throw new InvalidOperationException(
                        $"{path}: broken managed reference at {target.name}.{property.propertyPath}.");
            }
        }

        static void CheckLegacyText(string path)
        {
            if (path.EndsWith(nameof(VisualScriptingProjectAudit) + ".cs", StringComparison.Ordinal))
                return;
            string extension = Path.GetExtension(path);
            if (extension is not (".cs" or ".asset" or ".prefab" or ".unity")) return;
            string text = File.ReadAllText(path);
            foreach (string legacyType in LegacyTypes)
                if (ContainsTypeName(text, legacyType))
                    throw new InvalidOperationException($"{path}: legacy type {legacyType} remains.");
        }

        static bool ContainsTypeName(string text, string typeName)
        {
            int index = 0;
            while ((index = text.IndexOf(typeName, index, StringComparison.Ordinal)) >= 0)
            {
                bool left = index == 0 || !IsIdentifier(text[index - 1]);
                int end = index + typeName.Length;
                bool right = end >= text.Length || !IsIdentifier(text[end]);
                if (left && right) return true;
                index = end;
            }
            return false;
        }

        static bool IsIdentifier(char value) => char.IsLetterOrDigit(value) || value == '_';
    }
}
#endif
