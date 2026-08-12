using UnityEngine;

namespace GameSystems.Stats
{
    [CreateAssetMenu(menuName = "Game Systems/Stats/Attribute", fileName = "ATTR_")]
    public sealed class AttributeDefinition : ScriptableObject
    {
        [SerializeField] string id = "attribute";
        [SerializeField] string displayName = "Attribute";
        [SerializeField, TextArea] string description;
        [SerializeField] Sprite icon;
        [SerializeField] Color color = Color.white;
        [SerializeField] float minimumValue;
        [SerializeField] StatDefinition maximumStat;
        [SerializeField, Min(0f)] float fallbackMaximum = 1f;
        [SerializeField, Range(0f, 1f)] float startPercent = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Color Color => color;
        public float MinimumValue => minimumValue;
        public StatDefinition MaximumStat => maximumStat;
        public float FallbackMaximum => fallbackMaximum;
        public float StartPercent => startPercent;
    }
}
