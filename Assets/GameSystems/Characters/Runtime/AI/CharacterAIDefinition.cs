using UnityEngine;
using GameSystems.Hooks;
using UnityEngine.Scripting.APIUpdating;
using GameSystems.Characters.AI;

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
        [SerializeField, Range(-1f, 1f), Tooltip("Fixed traversal direction. Zero follows the current target.")]
        float traversalDirection;
        [SerializeField] CharacterAITraversalSettings traversal = new();
        [SerializeField] CharacterBehaviorTree behaviorTree;

        public float DecisionInterval => Mathf.Max(.01f, decisionInterval);
        public float DetectionRadius => detectionRadius;
        public HookId TargetHook => targetHook;
        public LayerMask LineOfSightMask => lineOfSightMask;
        public float TraversalDirection => Mathf.Sign(traversalDirection);
        public CharacterAITraversalSettings Traversal => traversal ??= new CharacterAITraversalSettings();
        public CharacterBehaviorTree BehaviorTree => behaviorTree;

        public void Configure(float interval, float radius, HookId target,
            float direction = 0f)
        {
            decisionInterval = Mathf.Max(.01f, interval);
            detectionRadius = Mathf.Max(0f, radius);
            targetHook = target;
            traversalDirection = Mathf.Clamp(direction, -1f, 1f);
        }

        public void ConfigureBehaviorTree(CharacterBehaviorTree tree) => behaviorTree = tree;
    }
}
