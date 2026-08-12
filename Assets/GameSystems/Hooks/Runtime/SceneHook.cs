using UnityEngine;

namespace GameSystems.Hooks
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Systems/Identity/Scene Hook")]
    public sealed class SceneHook : MonoBehaviour
    {
        [SerializeField] HookId identity;
        bool registered;
        public HookId Identity => identity;

        public void Configure(HookId value)
        {
            if (registered) HookRegistry.Unregister(this);
            identity = value;
            if (isActiveAndEnabled) registered = HookRegistry.Register(this);
        }

        void OnEnable() => registered = HookRegistry.Register(this);
        void OnDisable() { if (registered) HookRegistry.Unregister(this); registered = false; }

        void OnValidate()
        {
            if (identity == null) return;
            name = name.Trim();
        }
    }
}
