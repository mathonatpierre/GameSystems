using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using GameSystems.Characters;

namespace GameSystems.Editor
{
    [InitializeOnLoad]
    internal static class GameSystemsIconRegistry
    {
        const string IconRoot = "Assets/GameSystems/Editor/Icons/";

        static readonly Dictionary<string, Texture2D> IconCache = new();
        static bool scheduled;

        static GameSystemsIconRegistry()
        {
            EditorApplication.delayCall += ApplyAllIcons;
            EditorApplication.projectChanged += ScheduleApply;
        }

        static void ScheduleApply()
        {
            if (scheduled) return;
            scheduled = true;
            EditorApplication.delayCall += ApplyAllIcons;
        }

        static void ApplyAllIcons()
        {
            scheduled = false;
            ApplyScriptIcons();
            ApplyAssetIcons();
        }

        static void ApplyScriptIcons()
        {
            string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/GameSystems" });
            for (int i = 0; i < guids.Length; i++)
            {
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guids[i]));
                Type type = script != null ? script.GetClass() : null;
                Texture2D icon = IconFor(type);
                if (icon != null) EditorGUIUtility.SetIconForObject(script, icon);
            }
        }

        static void ApplyAssetIcons()
        {
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });
            for (int i = 0; i < guids.Length; i++)
            {
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                Texture2D icon = asset != null ? IconFor(asset.GetType()) : null;
                if (icon != null) EditorGUIUtility.SetIconForObject(asset, icon);
            }
        }

        static Texture2D IconFor(Type type)
        {
            if (type == null || string.IsNullOrEmpty(type.Namespace) ||
                !type.Namespace.StartsWith("GameSystems.", StringComparison.Ordinal)) return null;

            string name = type.Name;
            string iconName;

            if (name == "AbilitySet") iconName = "ability-set";
            else if (name == "CharacterAIDefinition") iconName = "character-ai";
            else if (name == "SequenceAbilityDefinition") iconName = "sequence-ability";
            else if (name == "ProceduralAnimationClip") iconName = "procedural-clip";
            else if (name == "AnimationClipAsset") iconName = "animation-clip";
            else if (name == "GameActionSequenceAsset") iconName = "action-sequence";
            else if (name == "HookId") iconName = "hook-id";
            else if (name.Contains("Locomotion", StringComparison.Ordinal)) iconName = "motor";
            else if (name.Contains("Reaction", StringComparison.Ordinal)) iconName = "reaction";
            else if (name.Contains("Death", StringComparison.Ordinal) ||
                     name.Contains("Respawn", StringComparison.Ordinal) ||
                     name.Contains("Victory", StringComparison.Ordinal)) iconName = "effect";
            else if (name == "LevelGenerationProfile") iconName = "level-generation";
            else if (name == "PlatformTypeDefinition") iconName = "platform-type";
            else if (name == "CharacterStatsDefinition") iconName = "character-stats";
            else if (name == "StatFormulaDefinition") iconName = "formula";
            else if (name == "StatDefinition") iconName = "stat";
            else if (name == "AttributeDefinition") iconName = "attribute";
            else if (name.Contains("Condition", StringComparison.Ordinal)) iconName = "condition";
            else if (name.Contains("Effect", StringComparison.Ordinal)) iconName = "effect";
            else if (name.Contains("InputMap", StringComparison.Ordinal)) iconName = "input-map";
            else if (name.Contains("Motor", StringComparison.Ordinal)) iconName = "motor";
            else if (name == "FeedbackSequence") iconName = "feedback-sequence";
            else if (name.Contains("Blend1D", StringComparison.Ordinal)) iconName = "blend-1d";
            else if (name.Contains("Blend2D", StringComparison.Ordinal)) iconName = "blend-2d";
            else if (name == "SceneHook") iconName = "scene-hook";
            else if (type.Namespace.StartsWith("GameSystems.Abilities", StringComparison.Ordinal)) iconName = "abilities";
            else if (type.Namespace.StartsWith("GameSystems.Feedbacks", StringComparison.Ordinal)) iconName = "feedbacks";
            else if (type.Namespace.StartsWith("GameSystems.Stats", StringComparison.Ordinal)) iconName = "stats";
            else if (type.Namespace.StartsWith("GameSystems.LevelGeneration", StringComparison.Ordinal)) iconName = "level-generation";
            else if (type.Namespace.StartsWith("GameSystems.Playables", StringComparison.Ordinal)) iconName = "playables";
            else if (type.Namespace.StartsWith("GameSystems.Hooks", StringComparison.Ordinal)) iconName = "hooks";
            else if (type.Namespace.StartsWith("GameSystems.Core", StringComparison.Ordinal)) iconName = "core";
            else return null;

            return LoadIcon(iconName);
        }

        static Texture2D LoadIcon(string name)
        {
            if (IconCache.TryGetValue(name, out Texture2D cached)) return cached;
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconRoot + name + ".png");
            if (icon == null) icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconRoot + Fallback(name) + ".png");
            IconCache[name] = icon;
            return icon;
        }

        static string Fallback(string name) => name switch
        {
            "character-ai" or "sequence-ability" => "abilities",
            "reaction" => "effect",
            "procedural-clip" or "animation-clip" or "animation-set" => "playables",
            "action-sequence" => "core",
            "hook-id" => "hooks",
            _ => name
        };
    }
}
