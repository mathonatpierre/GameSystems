using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Stats
{
    [DisallowMultipleComponent]
    public sealed class CharacterResources : MonoBehaviour
    {
        [Serializable]
        struct ResourceAmount
        {
            public ResourceDefinition resource;
            public int amount;
        }

        const string SavePrefix = "GameSystems.Resource.";
        [SerializeField] List<ResourceAmount> amounts = new();
        public event Action<ResourceDefinition, int, int> Changed;

        public int Get(ResourceDefinition resource)
        {
            if (resource == null) return 0;
            int index = Find(resource);
            if (index >= 0) return amounts[index].amount;
            int saved = Mathf.Clamp(PlayerPrefs.GetInt(SaveKey(resource), 0), 0,
                resource.Maximum > 0 ? resource.Maximum : int.MaxValue);
            amounts.Add(new ResourceAmount { resource = resource, amount = saved });
            return saved;
        }

        public int Add(ResourceDefinition resource, int amount)
        {
            if (resource == null || amount <= 0) return 0;
            int previous = Get(resource);
            int next = resource.Maximum > 0
                ? Mathf.Min(resource.Maximum, previous + amount)
                : previous + amount;
            if (next == previous) return 0;
            Store(resource, next);
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
            Store(resource, next);
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
            Store(resource, next);
            Changed?.Invoke(resource, previous, next);
        }

        public void Clear(ResourceDefinition resource) => Set(resource, 0);

        int Find(ResourceDefinition resource)
        {
            for (int i = 0; i < amounts.Count; i++)
                if (amounts[i].resource == resource) return i;
            return -1;
        }

        void Store(ResourceDefinition resource, int amount)
        {
            int index = Find(resource);
            ResourceAmount entry = new() { resource = resource, amount = amount };
            if (index >= 0) amounts[index] = entry;
            else amounts.Add(entry);
            PlayerPrefs.SetInt(SaveKey(resource), amount);
            PlayerPrefs.Save();
        }

        static string SaveKey(ResourceDefinition resource) => SavePrefix + resource.Id;
    }
}
