using System;
using UnityEngine;
namespace GameSystems.Sequencing
{
    public readonly struct GameActionContext
    {
        readonly object[] values;
        public GameActionContext(UnityEngine.Object owner, params object[] values)
            : this(owner, owner, null, values) { }

        public GameActionContext(UnityEngine.Object owner, UnityEngine.Object self,
            UnityEngine.Object target, params object[] values)
        {
            Owner = owner;
            Self = self != null ? self : owner;
            Target = target;
            this.values = values;
        }

        public UnityEngine.Object Owner { get; }
        public UnityEngine.Object Self { get; }
        public UnityEngine.Object Target { get; }

        public GameActionContext WithSelfTarget(UnityEngine.Object self,
            UnityEngine.Object target) => new(Owner, self, target, values);

        public GameActionContext WithTarget(UnityEngine.Object target) =>
            new(Owner, Self, target, values);

        public GameActionContext WithValue(object value)
        {
            int count = values?.Length ?? 0;
            object[] next = new object[count + 1];
            if (count > 0) Array.Copy(values, next, count);
            next[count] = value;
            return new GameActionContext(Owner, Self, Target, next);
        }
        public bool TryGet<T>(out T value)
        {
            if (values != null) for (int i = 0; i < values.Length; i++)
                if (values[i] is T candidate) { value = candidate; return true; }
            value = default; return false;
        }
        public T Get<T>() => TryGet(out T value) ? value : throw new InvalidOperationException($"Action context does not contain {typeof(T).Name}.");
    }
}
