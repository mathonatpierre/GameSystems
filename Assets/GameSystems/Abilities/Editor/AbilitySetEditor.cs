using System.Collections.Generic;
using System.Linq;
using GameSystems.Editor;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Abilities.Editor
{
    [CustomEditor(typeof(AbilitySet))]
    public sealed class AbilitySetEditor : UnityEditor.Editor
    {
        bool showAbilities = true;
        bool showValidation = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GameSystemsInspectorUI.InlineScriptableObjectList(
                serializedObject.FindProperty("abilities"),
                typeof(AbilityDefinition),
                "Abilities");
            serializedObject.ApplyModifiedProperties();

            AbilitySet set = (AbilitySet)target;
            List<string> errors = new();
            List<string> warnings = new();
            HashSet<AbilityDefinition> known = new();
            for (int i = 0; i < set.Abilities.Count; i++)
            {
                AbilityDefinition ability = set.Abilities[i];
                if (ability == null) { errors.Add($"Ability slot {i + 1} is missing."); continue; }
                if (!known.Add(ability)) errors.Add($"{ability.name} appears more than once.");
            }
            foreach (AbilityDefinition ability in known)
            {
                AddCategoryWarnings(ability, warnings);
                HashSet<(AbilityTransitionTrigger trigger, int priority)> schedulingSlots = new();
                foreach (AbilityTransitionDefinition transition in ability.Transitions)
                {
                    if (transition == null) { errors.Add($"{ability.name} has a missing transition."); continue; }
                    if (transition.Target != null && !known.Contains(transition.Target))
                        errors.Add($"{ability.name} → {transition.Target.name}: target is not in this Ability Set.");
                    if (transition.Conditions.Length == 0 && transition.Trigger == AbilityTransitionTrigger.WhileActive)
                        warnings.Add($"{ability.name}/{transition.Label}: unconditional WhileActive transition.");
                    if (!schedulingSlots.Add((transition.Trigger, transition.Priority)))
                        warnings.Add($"{ability.name}: multiple {transition.Trigger} transitions use priority {transition.Priority}. Array order will decide the winner.");
                }
            }
            FindImmediateCycles(known, errors);
            DrawSummary(known, errors.Count, warnings.Count);
            DrawAbilityOverview(set);
            DrawValidation(errors, warnings);
        }

        static void DrawSummary(HashSet<AbilityDefinition> abilities, int errors, int warnings)
        {
            int locomotion = abilities.Count(item => item != null && item.Category == AbilityCategory.Locomotion);
            int reactions = abilities.Count(item => item != null && item.Category == AbilityCategory.Reaction);
            int regular = abilities.Count(item => item != null && item.Category == AbilityCategory.Ability);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GameSystemsInspectorUI.Pill($"{abilities.Count} total", new Color(.42f, .55f, .95f));
                GameSystemsInspectorUI.Pill($"{locomotion} locomotion", new Color(.36f, .72f, .76f));
                GameSystemsInspectorUI.Pill($"{regular} abilities", new Color(.48f, .75f, .42f));
                GameSystemsInspectorUI.Pill($"{reactions} reactions", new Color(.95f, .58f, .36f));
                GUILayout.FlexibleSpace();
                GameSystemsInspectorUI.Pill(errors == 0 ? "valid" : $"{errors} errors",
                    errors == 0 ? new Color(.25f, .68f, .38f) : new Color(.92f, .36f, .32f));
                if (warnings > 0) GameSystemsInspectorUI.Pill($"{warnings} warnings", new Color(.95f, .74f, .32f));
            }
        }

        void DrawAbilityOverview(AbilitySet set)
        {
            if (!GameSystemsInspectorUI.Foldout(ref showAbilities, "Overview", $"{set.Abilities.Count} entries")) return;
            DrawAbilityGroup("Locomotion", set, AbilityCategory.Locomotion);
            DrawAbilityGroup("Abilities", set, AbilityCategory.Ability);
            DrawAbilityGroup("Reactions", set, AbilityCategory.Reaction);
            GameSystemsInspectorUI.EndFoldout();
        }

        static void DrawAbilityGroup(string title, AbilitySet set, AbilityCategory category)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            bool any = false;
            for (int i = 0; i < set.Abilities.Count; i++)
            {
                AbilityDefinition ability = set.Abilities[i];
                if (ability == null || ability.Category != category) continue;
                any = true;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(24f));
                    EditorGUILayout.ObjectField(ability, typeof(AbilityDefinition), false);
                    EditorGUILayout.LabelField(ability.AutoStart ? "Auto Start" : "Requested", GameSystemsInspectorUI.SmallMutedStyle, GUILayout.Width(72f));
                    EditorGUILayout.LabelField($"P {ability.Priority}", GameSystemsInspectorUI.SmallMutedStyle, GUILayout.Width(38f));
                    EditorGUILayout.LabelField($"{ability.Transitions.Length} trans", GameSystemsInspectorUI.SmallMutedStyle, GUILayout.Width(58f));
                }
            }

            if (!any) EditorGUILayout.LabelField("None", GameSystemsInspectorUI.SmallMutedStyle);
            EditorGUILayout.Space(2f);
        }

        void DrawValidation(List<string> errors, List<string> warnings)
        {
            if (!GameSystemsInspectorUI.Foldout(ref showValidation, "Validation",
                    errors.Count == 0 && warnings.Count == 0 ? "clean" : $"{errors.Count} errors, {warnings.Count} warnings")) return;
            if (errors.Count == 0 && warnings.Count == 0)
                EditorGUILayout.HelpBox("Ability Set is valid.", MessageType.Info);
            foreach (string error in errors) EditorGUILayout.HelpBox(error, MessageType.Error);
            foreach (string warning in warnings) EditorGUILayout.HelpBox(warning, MessageType.Warning);
            GameSystemsInspectorUI.EndFoldout();
        }

        static void FindImmediateCycles(HashSet<AbilityDefinition> abilities, List<string> errors)
        {
            Dictionary<AbilityDefinition, List<AbilityDefinition>> edges = abilities.ToDictionary(
                ability => ability,
                ability => ability.Transitions
                    .Where(transition => transition != null &&
                                         transition.Trigger == AbilityTransitionTrigger.WhileActive &&
                                         transition.Conditions.Length == 0 &&
                                         transition.CompleteSource &&
                                         transition.Target != null)
                    .Select(transition => transition.Target)
                    .Where(abilities.Contains)
                    .ToList());

            HashSet<AbilityDefinition> visiting = new();
            HashSet<AbilityDefinition> visited = new();
            Stack<AbilityDefinition> path = new();
            foreach (AbilityDefinition ability in abilities)
                Visit(ability, edges, visiting, visited, path, errors);
        }

        static void AddCategoryWarnings(AbilityDefinition ability, List<string> warnings)
        {
            if (ability.Category == AbilityCategory.Locomotion && !ability.AutoStart)
                warnings.Add($"{ability.name}: locomotion usually starts automatically.");
        }

        static void Visit(
            AbilityDefinition ability,
            Dictionary<AbilityDefinition, List<AbilityDefinition>> edges,
            HashSet<AbilityDefinition> visiting,
            HashSet<AbilityDefinition> visited,
            Stack<AbilityDefinition> path,
            List<string> errors)
        {
            if (visited.Contains(ability)) return;
            if (!visiting.Add(ability))
            {
                string cycle = string.Join(" → ", path.Reverse().Select(item => item.name).Append(ability.name));
                string message = $"Immediate unconditional transition cycle: {cycle}.";
                if (!errors.Contains(message)) errors.Add(message);
                return;
            }

            path.Push(ability);
            foreach (AbilityDefinition target in edges[ability])
                Visit(target, edges, visiting, visited, path, errors);
            path.Pop();
            visiting.Remove(ability);
            visited.Add(ability);
        }
    }
}
