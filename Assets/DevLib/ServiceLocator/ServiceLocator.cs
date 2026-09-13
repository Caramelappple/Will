using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevLib.ServiceLocator
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void InitializeServiceLocator()
        {
            _services.Clear();
        }

        public static void Register<T>(T service)
        {
            _services[typeof(T)] = service;
            Debug.Log($"Registering {typeof(T).Name}");
        }

        public static void Unregister<T>()
        {
            _services.Remove(typeof(T));
            Debug.Log($"Unregistering {typeof(T).Name}");
        }

        public static T Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out object service))
                return (T)service;
            
            Debug.LogWarning($"{typeof(T).Name} Not Found)");
            return default;
        }
    }
}