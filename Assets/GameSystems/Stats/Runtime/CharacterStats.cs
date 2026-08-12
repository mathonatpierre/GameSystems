using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Stats
{
    [DisallowMultipleComponent]
    public sealed class CharacterStats : MonoBehaviour, IStatProvider, IAttributeProvider
    {
        [SerializeField] CharacterStatsDefinition definition;
        readonly Dictionary<StatDefinition, RuntimeStat> stats = new();
        readonly Dictionary<AttributeDefinition, RuntimeAttribute> attributes = new();
        readonly List<TimedModifier> timedModifiers = new();
        int nextModifierHandle = 1;

        struct TimedModifier
        {
            public StatDefinition stat;
            public StatModifierHandle handle;
            public float remainingTime;
        }

        public CharacterStatsDefinition Definition => definition;
        public IEnumerable<RuntimeStat> RuntimeStats => stats.Values;
        public IEnumerable<RuntimeAttribute> RuntimeAttributes => attributes.Values;
        public event Action<StatDefinition, float> StatChanged;
        public event Action<AttributeDefinition, float, float> AttributeChanged;

        public void Configure(CharacterStatsDefinition value)
        {
            definition = value;
            Rebuild();
        }

        void Awake() => Rebuild();

        public void Rebuild()
        {
            stats.Clear();
            attributes.Clear();
            timedModifiers.Clear();
            if (definition == null) return;
            foreach (StatDefinition item in definition.Stats)
                if (item != null && !stats.ContainsKey(item)) stats.Add(item, new RuntimeStat(item));
            foreach (AttributeDefinition item in definition.Attributes)
                if (item != null && !attributes.ContainsKey(item)) attributes.Add(item, new RuntimeAttribute(item, this));
        }

        public RuntimeStat GetStat(StatDefinition stat) => stat != null && stats.TryGetValue(stat, out RuntimeStat value) ? value : null;
        public RuntimeAttribute GetAttribute(AttributeDefinition attribute) => attribute != null && attributes.TryGetValue(attribute, out RuntimeAttribute value) ? value : null;
        public float GetStatValue(StatDefinition stat) => GetStat(stat)?.Value ?? 0f;

        public StatDefinition FindStat(string id)
        {
            foreach (StatDefinition item in stats.Keys)
                if (string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)) return item;
            return null;
        }

        public AttributeDefinition FindAttribute(string id)
        {
            foreach (AttributeDefinition item in attributes.Keys)
                if (string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)) return item;
            return null;
        }

        public bool Set(AttributeDefinition attribute, float value)
        {
            RuntimeAttribute runtime = GetAttribute(attribute);
            if (runtime == null) return false;
            float previous = runtime.Current;
            runtime.Set(value);
            if (!Mathf.Approximately(previous, runtime.Current)) AttributeChanged?.Invoke(attribute, previous, runtime.Current);
            return true;
        }

        public bool Change(AttributeDefinition attribute, float delta)
        {
            RuntimeAttribute runtime = GetAttribute(attribute);
            if (runtime == null) return false;
            return Set(attribute, runtime.Current + delta);
        }

        public void AddModifier(StatDefinition stat, StatModifier modifier)
        {
            RuntimeStat runtime = GetStat(stat);
            if (runtime == null) return;
            runtime.AddModifier(modifier);
            StatChanged?.Invoke(stat, runtime.Value);
            ClampDependentAttributes(stat);
        }

        public StatModifierHandle AddModifier(StatDefinition stat, StatModifier modifier, float duration)
        {
            RuntimeStat runtime = GetStat(stat);
            if (runtime == null) return default;
            var handle = new StatModifierHandle(nextModifierHandle++);
            runtime.AddModifier(handle, modifier);
            if (duration > 0f)
                timedModifiers.Add(new TimedModifier { stat = stat, handle = handle, remainingTime = duration });
            StatChanged?.Invoke(stat, runtime.Value);
            ClampDependentAttributes(stat);
            return handle;
        }

        public bool RemoveModifier(StatModifierHandle handle)
        {
            if (!handle.IsValid) return false;
            for (int i = 0; i < timedModifiers.Count; i++)
                if (timedModifiers[i].handle == handle)
                {
                    timedModifiers.RemoveAt(i);
                    break;
                }

            foreach (RuntimeStat runtime in stats.Values)
            {
                if (!runtime.RemoveModifier(handle)) continue;
                StatChanged?.Invoke(runtime.Definition, runtime.Value);
                ClampDependentAttributes(runtime.Definition);
                return true;
            }

            return false;
        }

        public void RemoveModifiersFrom(object source)
        {
            foreach (RuntimeStat runtime in stats.Values)
            {
                runtime.RemoveModifiersFrom(source);
                StatChanged?.Invoke(runtime.Definition, runtime.Value);
                ClampDependentAttributes(runtime.Definition);
            }
        }

        void Update()
        {
            if (timedModifiers.Count == 0) return;
            float deltaTime = Time.deltaTime;
            for (int i = timedModifiers.Count - 1; i >= 0; i--)
            {
                TimedModifier modifier = timedModifiers[i];
                modifier.remainingTime -= deltaTime;
                if (modifier.remainingTime > 0f)
                {
                    timedModifiers[i] = modifier;
                    continue;
                }

                timedModifiers.RemoveAt(i);
                RuntimeStat runtime = GetStat(modifier.stat);
                if (runtime == null || !runtime.RemoveModifier(modifier.handle)) continue;
                StatChanged?.Invoke(runtime.Definition, runtime.Value);
                ClampDependentAttributes(runtime.Definition);
            }
        }

        void ClampDependentAttributes(StatDefinition stat)
        {
            foreach (RuntimeAttribute attribute in attributes.Values)
            {
                if (attribute.Definition.MaximumStat != stat) continue;
                float previous = attribute.Current;
                attribute.Set(previous);
                if (!Mathf.Approximately(previous, attribute.Current))
                    AttributeChanged?.Invoke(attribute.Definition, previous, attribute.Current);
            }
        }
    }
}
