using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Playables
{
    public sealed class PlayableAnimationContext
    {
        readonly Dictionary<string, float> floats = new(System.StringComparer.Ordinal);
        PlayableAnimationBindings bindings;
        public GameObject Owner { get; private set; }
        public bool HasAnimatorOutput { get; private set; }

        internal void Configure(GameObject owner, PlayableAnimationBindings value, bool hasAnimatorOutput)
        { Owner = owner; bindings = value; HasAnimatorOutput = hasAnimatorOutput; }
        public Transform ResolveBinding(string id)
        {
            if (bindings == null && Owner != null) bindings = Owner.GetComponent<PlayableAnimationBindings>();
            return bindings != null ? bindings.Resolve(id) : null;
        }

        public void SetFloat(string id, float value)
        {
            if (!string.IsNullOrWhiteSpace(id)) floats[id] = value;
        }

        public float GetFloat(string id, float fallback = 0f) =>
            !string.IsNullOrWhiteSpace(id) && floats.TryGetValue(id, out float value) ? value : fallback;

        public void Clear() => floats.Clear();
    }
}
