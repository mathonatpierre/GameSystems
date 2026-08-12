using System;
using GameSystems.Actions;
using UnityEngine;

namespace GameSystems.Abilities
{
    [Serializable]
    public sealed class CharacterAIDecision
    {
        [SerializeField] string label = "Decision";
        [SerializeField] AbilityDefinition ability;
        [SerializeField] int priority;
        [SerializeField, Min(0f)] float minimumInterval;
        [SerializeField] GameConditionMode conditionMode = GameConditionMode.All;
        [SerializeReference] GameCondition[] conditions;

        [NonSerialized] double nextAllowedAt;
        [NonSerialized] bool selected;

        public string Label => label;
        public AbilityDefinition Ability => ability;
        public int Priority => priority;
        public GameConditionMode ConditionMode => conditionMode;
        public GameCondition[] Conditions => conditions ?? Array.Empty<GameCondition>();
        public bool DebugSelected => selected;

        public bool Evaluate(in CharacterAIContext aiContext, double now)
        {
            selected = false;
            if (ability == null || now < nextAllowedAt) return false;
            GameActionContext context = new(aiContext.Character.Owner, aiContext,
                aiContext.Character, aiContext.Controller);
            return GameConditionEvaluator.Evaluate(Conditions, conditionMode, context);
        }

        public void MarkSelected(double now)
        {
            selected = true;
            nextAllowedAt = now + minimumInterval;
        }

        public void ClearDebug() => selected = false;
    }
}
