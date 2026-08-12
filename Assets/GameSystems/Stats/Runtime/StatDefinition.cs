using UnityEngine;

namespace GameSystems.Stats
{
    [CreateAssetMenu(menuName = "Game Systems/Stats/Stat", fileName = "STAT_")]
    public sealed class StatDefinition : ScriptableObject
    {
        [SerializeField] string id = "stat";
        [SerializeField] string displayName = "Stat";
        [SerializeField, TextArea] string description;
        [SerializeField] Sprite icon;
        [SerializeField] Color color = Color.white;
        [SerializeField] float baseValue = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Color Color => color;
        public float BaseValue => baseValue;
    }
}
