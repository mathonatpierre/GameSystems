#if UNITY_EDITOR
using System;
using GameSystems.Sequencing;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Abilities.Editor
{
    public static class AbilitySequenceMigrationAudit
    {
        [MenuItem("Game Systems/Abilities/Audit Sequence Migration")]
        public static void Run()
        {
            AbilitySet set = AssetDatabase.LoadAssetAtPath<AbilitySet>(
                "Assets/Lennie/Data/Characters/Lennie/ABILITYSET_Lennie.asset");
            if (set == null) throw new InvalidOperationException("Lennie ability set is missing.");
            int conditions = 0, actions = 0;
            int transitions = 0;
            foreach (AbilityDefinition ability in set.Abilities)
            {
                if (ability is not SequenceAbilityDefinition sequence)
                    throw new InvalidOperationException($"{ability.name} is still {ability.GetType().Name}.");
                for (int i = 0; i < sequence.Sequence.Conditions.Length; i++)
                    if (sequence.Sequence.Conditions[i] == null) throw new InvalidOperationException($"{ability.name} condition {i} is missing.");
                for (int i = 0; i < sequence.Sequence.Actions.Length; i++)
                    if (sequence.Sequence.Actions[i] == null) throw new InvalidOperationException($"{ability.name} action {i} is missing.");
                conditions += sequence.Sequence.Conditions.Length;
                actions += sequence.Sequence.Actions.Length;
                foreach (AbilityTransitionDefinition transition in ability.Transitions)
                {
                    if (transition == null) throw new InvalidOperationException($"{ability.name} has a missing transition.");
                    for (int i = 0; i < transition.Conditions.Length; i++)
                        if (transition.Conditions[i] == null) throw new InvalidOperationException($"{ability.name}/{transition.Label} condition {i} is missing.");
                    for (int i = 0; i < transition.Actions.Length; i++)
                        if (transition.Actions[i] == null) throw new InvalidOperationException($"{ability.name}/{transition.Label} action {i} is missing.");
                    conditions += transition.Conditions.Length;
                    actions += transition.Actions.Length;
                    transitions++;
                }
            }
            if (!CharacterCapabilityResolver.TryResolve(set, 1.1f, .18f, .25f,
                    out CharacterMovementCapabilities capabilities))
                throw new InvalidOperationException("Movement capabilities could not be resolved.");
            if (!capabilities.HasJump || !capabilities.HasWallJump || capabilities.GroundSpeed <= 0f || capabilities.AirSpeed <= 0f)
                throw new InvalidOperationException("Migrated movement capabilities are incomplete.");
            Debug.Log($"[Ability Migration Audit] PASS · {set.Abilities.Count} sequence abilities · {transitions} transitions · {conditions} conditions · {actions} actions · jump {capabilities.HeldJumpHeight:0.##}m · wall {capabilities.WallJumpDistance:0.##}m");
        }
    }
}
#endif
