using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Hooks
{
    public static class HookRegistry
    {
        static readonly Dictionary<HookId, SceneHook> Hooks = new();
        public static event Action<HookId, GameObject> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Hooks.Clear(); Changed = null; }

        internal static bool Register(SceneHook hook)
        {
            if (hook == null || hook.Identity == null) return false;
            if (Hooks.TryGetValue(hook.Identity, out SceneHook current) && current != null && current != hook)
            {
                Debug.LogError($"Duplicate hook '{hook.Identity.name}': '{current.name}' and '{hook.name}'.", hook);
                return false;
            }
            Hooks[hook.Identity] = hook;
            Changed?.Invoke(hook.Identity, hook.gameObject);
            return true;
        }

        internal static void Unregister(SceneHook hook)
        {
            if (hook == null || hook.Identity == null) return;
            if (!Hooks.TryGetValue(hook.Identity, out SceneHook current) || current != hook) return;
            Hooks.Remove(hook.Identity);
            Changed?.Invoke(hook.Identity, null);
        }

        public static bool TryGet(HookId id, out GameObject value)
        {
            value = null;
            if (id == null || !Hooks.TryGetValue(id, out SceneHook hook) || hook == null) return false;
            value = hook.gameObject;
            return true;
        }

        public static GameObject Get(HookId id) => TryGet(id, out GameObject value) ? value : null;
        public static T GetComponent<T>(HookId id) where T : Component
            => TryGet(id, out GameObject value) ? value.GetComponent<T>() : null;

        public static T GetFirstComponent<T>() where T : Component
        {
            T result = null;
            foreach (SceneHook hook in Hooks.Values)
            {
                if (hook == null || !hook.TryGetComponent(out T candidate)) continue;
                if (result != null && result != candidate)
                {
                    Debug.LogError($"More than one registered hook exposes {typeof(T).Name}. Resolve it with a HookId instead.");
                    return null;
                }
                result = candidate;
            }
            return result;
        }
    }
}
