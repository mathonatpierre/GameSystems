using System;
using System.Collections.Generic;

namespace GameSystems.Core
{
    public sealed class GameServiceRegistry
    {
        readonly Dictionary<Type, object> services = new();

        public void Register<T>(T service) where T : class
        {
            if (service == null) services.Remove(typeof(T));
            else services[typeof(T)] = service;
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            if (services.TryGetValue(typeof(T), out object value) && value is T typed)
            {
                service = typed;
                return true;
            }

            service = null;
            return false;
        }

        public T Resolve<T>() where T : class
            => TryResolve(out T service) ? service : null;
    }
}
