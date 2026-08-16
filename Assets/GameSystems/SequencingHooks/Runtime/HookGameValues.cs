using System;
using GameSystems.Hooks;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Sequencing.Hooks
{
    [Serializable]
    public sealed class HookGameObjectValue : GameObjectValue
    {
        [SerializeField] HookId hook;
        public override string Summary => hook != null ? $"Hook {hook.name}" : "Missing hook";
        public override GameObject Get(in GameActionContext context) => HookRegistry.Get(hook);
    }
}
