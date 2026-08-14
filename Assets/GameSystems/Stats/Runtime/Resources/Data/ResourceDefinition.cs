using UnityEngine;

namespace GameSystems.Stats
{
    [CreateAssetMenu(menuName = "Game Systems/Stats/Resource", fileName = "RESOURCE_")]
    public sealed class ResourceDefinition : ScriptableObject
    {
        [SerializeField] string id = "resource";
        [SerializeField] string displayName = "Resource";
        [SerializeField, TextArea] string description;
        [SerializeField] Sprite icon;
        [SerializeField] Color color = Color.white;
        [SerializeField, Min(0)] int maximum;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Color Color => color;
        public int Maximum => maximum;

        public void Configure(string resourceId, string label, int maximumAmount = 0)
        {
            id = resourceId;
            displayName = label;
            maximum = Mathf.Max(0, maximumAmount);
        }
    }
}
