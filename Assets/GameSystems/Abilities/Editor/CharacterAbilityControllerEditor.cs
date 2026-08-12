#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GameSystems.Editor;
using GameSystems.Stats;

namespace GameSystems.Abilities.Editor
{
    [CustomEditor(typeof(CharacterAbilityController))]
    public sealed class CharacterAbilityControllerEditor : UnityEditor.Editor
    {
        static CharacterAbilityController debugCharacter;
        static double debugDropAt;
        bool showRuntime = true;
        bool showActive = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GameSystemsInspectorUI.InlineScriptableObject(
                serializedObject.FindProperty("abilitySet"), typeof(AbilitySet), "Ability Set");
            serializedObject.ApplyModifiedProperties();

            CharacterAbilityController controller = (CharacterAbilityController)target;

            if (!Application.isPlaying)
            {
                GameSystemsInspectorUI.Header("Runtime Debug", "Controller state appears in Play Mode.");
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GameSystemsInspectorUI.Pill("PLAY MODE", new Color(.25f, .7f, .42f));
                GameSystemsInspectorUI.Pill($"{controller.CountActive(AbilityCategory.Locomotion)} locomotion", new Color(.36f, .72f, .76f));
                GameSystemsInspectorUI.Pill($"{controller.CountActive(AbilityCategory.Ability)} abilities", new Color(.48f, .75f, .42f));
                GameSystemsInspectorUI.Pill($"{controller.CountActive(AbilityCategory.Reaction)} reactions", new Color(.95f, .58f, .36f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset", GUILayout.Width(70f))) controller.ResetAll();
                if (GUILayout.Button("Lethal Fall", GUILayout.Width(88f))) ForceLethalFall();
            }

            if (GameSystemsInspectorUI.Foldout(ref showRuntime, "Last Request",
                    controller.LastRequestResult.ToString()))
            {
                Row("Exclusive Authorities", controller.ExclusiveAuthorities.ToString());
                Row("Ability", controller.LastRequestedAbility != null ? controller.LastRequestedAbility.name : "—");
                Row("Result", controller.LastRequestResult.ToString());
                Row("Transition", string.IsNullOrEmpty(controller.LastTransitionLabel) ? "—" : controller.LastTransitionLabel);
                Row("Source", controller.LastTransitionSource != null ? controller.LastTransitionSource.name : "—");
                Row("Target", controller.LastTransitionTarget != null ? controller.LastTransitionTarget.name : "Complete");
                GameSystemsInspectorUI.EndFoldout();
            }

            if (GameSystemsInspectorUI.Foldout(ref showActive, "Active Abilities",
                    controller.ActiveAbilities.Count == 0 ? "none" : $"{controller.ActiveAbilities.Count} running"))
            {
                DrawRuntimeGroup("Locomotion", controller.ActiveAbilities, AbilityCategory.Locomotion);
                DrawRuntimeGroup("Abilities", controller.ActiveAbilities, AbilityCategory.Ability);
                DrawRuntimeGroup("Reactions", controller.ActiveAbilities, AbilityCategory.Reaction);
                GameSystemsInspectorUI.EndFoldout();
            }

            Repaint();
        }

        static void DrawRuntimeGroup(string label,
            System.Collections.Generic.IReadOnlyList<AbilityRuntime> items, AbilityCategory category)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            bool any = false;
            for (int i = 0; i < items.Count; i++)
            {
                AbilityRuntime runtime = items[i];
                if (runtime.Definition.Category != category) continue;
                any = true;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(runtime.Definition.name, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(runtime.Phase.ToString(), GameSystemsInspectorUI.SmallMutedStyle, GUILayout.Width(72f));
                    EditorGUILayout.LabelField(runtime.ActiveTime.ToString("0.000 s"), GameSystemsInspectorUI.SmallMutedStyle, GUILayout.Width(70f));
                    EditorGUILayout.LabelField(runtime.HasPendingTransition ? "pending" : runtime.Definition.ExclusiveAuthority.ToString(), GameSystemsInspectorUI.SmallMutedStyle, GUILayout.Width(92f));
                }
            }
            if (!any) EditorGUILayout.LabelField("None", GameSystemsInspectorUI.SmallMutedStyle);
        }

        static void Row(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GameSystemsInspectorUI.SmallMutedStyle, GUILayout.Width(130f));
                EditorGUILayout.LabelField(value);
            }
        }

        [MenuItem("Game Systems/Diagnostics/Force Lethal Fall")]
        static void ForceLethalFall()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode first."); return; }
            debugCharacter = Object.FindAnyObjectByType<CharacterAbilityController>();
            if (debugCharacter == null) { Debug.LogError("No CharacterAbilityController found."); return; }
            if (debugCharacter.Motor is ICharacterMotorControl control) control.SetVerticalVelocity(-2f);
            debugDropAt = EditorApplication.timeSinceStartup + .3d;
            EditorApplication.update -= ApplyDebugDrop;
            EditorApplication.update += ApplyDebugDrop;
        }

        static void ApplyDebugDrop()
        {
            if (EditorApplication.timeSinceStartup < debugDropAt || debugCharacter == null) return;
            EditorApplication.update -= ApplyDebugDrop;
            debugCharacter.transform.position += Vector3.down * 12f;
            Physics.SyncTransforms();
            CharacterStats stats = debugCharacter.GetComponent<CharacterStats>();
            RuntimeAttribute health = stats?.GetAttribute(stats.Definition?.PrimaryHealth);
            Debug.Log($"[Ability Debug] Forced lethal fall. Health before transition: {health?.Current}");
        }
    }
}
#endif
