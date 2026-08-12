using System;
using UnityEngine;

namespace GameSystems.Hooks
{
    [Serializable]
    public sealed class HookReference
    {
        [SerializeField] HookId hook;
        [SerializeField] GameObject directReference;
        public HookId Hook => hook;
        public GameObject Resolve() => hook != null ? HookRegistry.Get(hook) : directReference;
        public T Resolve<T>() where T : Component => Resolve() != null ? Resolve().GetComponent<T>() : null;
    }
}
