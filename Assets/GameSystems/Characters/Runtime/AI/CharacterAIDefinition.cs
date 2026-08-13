using System;
using UnityEngine;
using GameSystems.Hooks;
using UnityEngine.Scripting.APIUpdating;

namespace GameSystems.Characters
{
    [MovedFrom(true, "GameSystems.Abilities", "GameSystems.Abilities", "CharacterAIDefinition")]
    [CreateAssetMenu(menuName = "Game Systems/Characters/AI Definition", fileName = "CHARAI_")]
    public sealed class CharacterAIDefinition : ScriptableObject
    {
        [SerializeField, Min(.01f)] float decisionInterval = .1f;
        [SerializeField, Min(0f)] float detectionRadius = 8f;
        [SerializeField, Tooltip("Optional stable identity used as this AI's primary target.")]
        HookId targetHook;
        [SerializeField] LayerMask lineOfSightMask = ~0;
        [SerializeField] CharacterAIDecision[] decisions;

        public float DecisionInterval => Mathf.Max(.01f, decisionInterval);
        public float DetectionRadius => detectionRadius;
        public HookId TargetHook => targetHook;
        public LayerMask LineOfSightMask => lineOfSightMask;
        public CharacterAIDecision[] Decisions => decisions ?? Array.Empty<CharacterAIDecision>();
    }
}
