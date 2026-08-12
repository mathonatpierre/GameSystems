using UnityEngine;
using System;
using System.Collections.Generic;

namespace GameSystems.Abilities
{
    public sealed class CharacterRuntimeContext
    {
        public GameObject Owner { get; }
        public Transform Transform => Owner.transform;
        public CharacterAbilityController Abilities { get; internal set; }
        public ICharacterMotor Motor { get; internal set; }
        readonly Dictionary<Type, object> services = new();

        public CharacterRuntimeContext(GameObject owner)
        {
            Owner = owner;
        }

        public T Get<T>() where T : class
        {
            // Interface lookups must not select a disabled input/command source.
            // This lets the same character prefab use player input in gameplay and
            // an AI source in an attract-mode/title scene without duplicating data.
            MonoBehaviour[] behaviours = Owner.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
                if (behaviours[i].enabled && behaviours[i] is T service)
                    return service;
            return Owner.GetComponent(typeof(T)) as T;
        }

        public void Bind<T>(T service) where T : class
        {
            if (service == null) services.Remove(typeof(T));
            else services[typeof(T)] = service;
        }

        public T Resolve<T>() where T : class
        {
            if (services.TryGetValue(typeof(T), out object value)) return value as T;
            return Get<T>();
        }
    }
}
