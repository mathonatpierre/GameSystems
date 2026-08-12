using UnityEngine;

namespace GameSystems.Hooks
{
    [CreateAssetMenu(menuName = "Game Systems/Identity/Hook", fileName = "HOOK_")]
    public sealed class HookId : ScriptableObject
    {
        [SerializeField, TextArea] string description;
        public string Description => description;
    }
}
