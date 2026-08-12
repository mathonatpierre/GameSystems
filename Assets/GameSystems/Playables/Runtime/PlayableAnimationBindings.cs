using System;
using UnityEngine;

namespace GameSystems.Playables
{
    [Serializable]
    public struct PlayableAnimationBinding
    {
        public string id;
        public Transform target;
    }

    [DisallowMultipleComponent]
    public sealed class PlayableAnimationBindings : MonoBehaviour
    {
        [SerializeField] PlayableAnimationBinding[] bindings;

        public Transform Resolve(string id)
        {
            if (bindings == null) return null;
            for (int i = 0; i < bindings.Length; i++)
                if (string.Equals(bindings[i].id, id, StringComparison.Ordinal)) return bindings[i].target;
            return null;
        }
    }
}
