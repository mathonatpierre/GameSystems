using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Stats
{
    [CreateAssetMenu(menuName = "Game Systems/Stats/Character Stats", fileName = "STATS_")]
    public sealed class CharacterStatsDefinition : ScriptableObject
    {
        [SerializeField] List<StatDefinition> stats = new();
        [SerializeField] List<AttributeDefinition> attributes = new();
        [SerializeField] AttributeDefinition primaryHealth;

        public IReadOnlyList<StatDefinition> Stats => stats;
        public IReadOnlyList<AttributeDefinition> Attributes => attributes;
        public AttributeDefinition PrimaryHealth => primaryHealth;
    }
}
