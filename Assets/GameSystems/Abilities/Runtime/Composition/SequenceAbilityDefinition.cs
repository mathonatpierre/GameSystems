using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Abilities
{
    [CreateAssetMenu(menuName = "Game Systems/Abilities/Sequence Ability", fileName = "ABILITY_")]
    public class SequenceAbilityDefinition : AbilityDefinition
    {
        [SerializeField, Tooltip("Conditions and ordered actions that implement this ability.")]
        GameActionSequence sequence = new();
        [SerializeField, Tooltip("Complete the ability automatically when its final action succeeds.")]
        bool completeWhenSequenceEnds = true;
        [SerializeField, Tooltip("Category used by presentation, debugging and active-ability queries.")]
        AbilityCategory category = AbilityCategory.Ability;
        [SerializeField, Tooltip("Restart the sequence when this ability is requested again while active.")]
        bool refreshWhileActive;

        public GameActionSequence Sequence => sequence;
        public bool CompleteWhenSequenceEnds => completeWhenSequenceEnds;
        public override AbilityCategory Category => category;
        public bool RefreshWhileActive => refreshWhileActive;
        public override AbilityRuntime CreateRuntime() => new SequenceAbilityRuntime();
    }
}
