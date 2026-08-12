using System;
using UnityEngine;
namespace GameSystems.Sequencing
{
    public readonly struct GameActionContext
    {
        readonly object[] values;
        public GameActionContext(UnityEngine.Object owner, params object[] values) { Owner = owner; this.values = values; }
        public UnityEngine.Object Owner { get; }
        public bool TryGet<T>(out T value)
        {
            if (values != null) for (int i = 0; i < values.Length; i++)
                if (values[i] is T candidate) { value = candidate; return true; }
            value = default; return false;
        }
        public T Get<T>() => TryGet(out T value) ? value : throw new InvalidOperationException($"Action context does not contain {typeof(T).Name}.");
    }
}
