using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Stats
{
    [DisallowMultipleComponent]
    public sealed class CharacterResources : MonoBehaviour
    {
        readonly Dictionary<ResourceDefinition, int> amounts = new();
        public event Action<ResourceDefinition, int, int> Changed;

        public int Get(ResourceDefinition resource) => resource != null &&
            amounts.TryGetValue(resource, out int value) ? value : 0;

        public int Add(ResourceDefinition resource, int amount)
        {
            if (resource == null || amount <= 0) return 0;
            int previous = Get(resource);
            int next = resource.Maximum > 0
                ? Mathf.Min(resource.Maximum, previous + amount)
                : previous + amount;
            if (next == previous) return 0;
            amounts[resource] = next;
            Changed?.Invoke(resource, previous, next);
            return next - previous;
        }

        public bool CanSpend(ResourceDefinition resource, int amount) =>
            resource != null && amount >= 0 && Get(resource) >= amount;

        public bool Spend(ResourceDefinition resource, int amount)
        {
            if (!CanSpend(resource, amount)) return false;
            int previous = Get(resource);
            int next = previous - amount;
            amounts[resource] = next;
            Changed?.Invoke(resource, previous, next);
            return true;
        }

        public void Set(ResourceDefinition resource, int amount)
        {
            if (resource == null) return;
            int previous = Get(resource);
            int next = Mathf.Clamp(amount, 0,
                resource.Maximum > 0 ? resource.Maximum : int.MaxValue);
            if (next == previous) return;
            amounts[resource] = next;
            Changed?.Invoke(resource, previous, next);
        }
    }
}
