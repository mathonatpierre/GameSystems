using UnityEngine;

namespace GameSystems.LevelGeneration
{
    [CreateAssetMenu(fileName = "PlatformType", menuName =
        "Game Systems/Level Generation/Platform Type")]
    public sealed class PlatformTypeDefinition : ScriptableObject
    {
        [SerializeField] string displayName = "Platform";
        [SerializeField] PlatformTypeId type;
        [SerializeField] GameObject prefab;
        [SerializeField] PlatformSelectionRules selection = new();
        [SerializeField] PlatformGeometryRules geometry = new();
        [SerializeField] PlatformSurfaceRules surface = new();
        [SerializeField] PlatformMotionRules motion = new();
        [SerializeField] PlatformLifecycleRules lifecycle = new();

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? name : displayName;
        public PlatformTypeId Type => type;
        public GameObject Prefab => prefab;
        public PlatformSelectionRules Selection => selection;
        public PlatformGeometryRules Geometry => geometry;
        public PlatformSurfaceRules Surface => surface;
        public PlatformMotionRules Motion => motion;
        public PlatformLifecycleRules Lifecycle => lifecycle;

        public void ConfigureIdentity(string label, PlatformTypeId id,
            GameObject sourcePrefab = null)
        {
            displayName = label;
            type = id;
            prefab = sourcePrefab;
        }

    }
}
