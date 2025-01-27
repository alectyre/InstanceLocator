using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Architecture
{
    public class InstanceContainer
    {
        private readonly Dictionary<Type, ArrayList> m_registeredInstances = new();
        private readonly Dictionary<Type, InstanceType> m_instanceTypes = new();
        private readonly Dictionary<Type, object> m_instanceRegisteredActions = new();
        private readonly Dictionary<Type, object> m_instanceUnregisteredActions = new();

        public void Register<T>(T instance, InstanceType instanceType = InstanceType.Singleton)
        {
            if (instance == null) return;

            Type type = typeof(T);

            // Check if the type is already registered
            if (m_registeredInstances.TryGetValue(type, out ArrayList instances))
            {
                // Check for preexisting singleton instance
                if (instanceType == InstanceType.Singleton && instances.Count > 0)
                {
                    // Allow the same instance to call Register multiple times without error
                    if (!instances.Contains(instance)) 
                        Debug.LogError($"Attempting to register instance {instance} of singleton type:" +
                                       $" {typeof(T)}, but a singleton instance is already registered.");
                    return;
                }

                // Check if the instance is already registered
                if (instances.Contains(instance)) return;

                // Add the instance to the registered instances list
                instances.Add(instance);
            }
            // If the type is not registered, add it to the dictionaries with the instance
            else
            {
                m_registeredInstances.Add(type, new ArrayList { instance });
                m_instanceTypes.Add(type, instanceType);
            }

            InvokeActions(m_instanceRegisteredActions, instance);
        }

        public void Unregister<T>(T instance)
        {
            Type type = typeof(T);

            if (!m_registeredInstances.TryGetValue(type, out ArrayList instances) || !instances.Contains(instance)) return;

            instances.Remove(instance);

            // Remove the type from the dictionaries if there are no instances left
            if (instances.Count == 0)
            {
                m_registeredInstances.Remove(type);
                m_instanceTypes.Remove(type);
            }

            InvokeActions(m_instanceUnregisteredActions, instance);
        }

        public T Get<T>()
        {
            if (m_registeredInstances.TryGetValue(typeof(T), out ArrayList instances) && instances.Count != 0)
                return (T)instances[0];

            Debug.LogWarning($"InstanceLocator: No instance of type {typeof(T)} is registered.");
            return default;
        }

        public IEnumerable<T> GetAll<T>()
        {
            // Log a warning if the type is registered as a singleton
            if (m_instanceTypes.TryGetValue(typeof(T), out InstanceType instanceType) &&
                instanceType == InstanceType.Singleton)
                Debug.LogWarning($"InstanceLocator: Attempting to get all instances of singleton type {typeof(T)}.");

            // Return an empty enumerable if no instances are registered
            if (!m_registeredInstances.TryGetValue(typeof(T), out ArrayList instances) || instances.Count == 0)
                return Enumerable.Empty<T>();

            return instances.Cast<T>();
        }

        public bool TryGet<T>(out T instance)
        {
            if (!m_registeredInstances.TryGetValue(typeof(T), out ArrayList instances) || instances.Count == 0)
            {
                instance = default;
                return false;
            }

            instance = (T)instances[0];
            return true;
        }

        public bool TryGetAll<T>(out IEnumerable<T> instances)
        {
            // Log a warning if the type is registered as a singleton
            if (m_instanceTypes.TryGetValue(typeof(T), out InstanceType instanceType) &&
                instanceType == InstanceType.Singleton)
                Debug.LogWarning($"InstanceLocator: Attempting to get all instances of singleton type {typeof(T)}.");

            // Return an empty enumerable if no instances are registered
            if (!m_registeredInstances.TryGetValue(typeof(T), out ArrayList arrayList) || arrayList.Count == 0)
            {
                instances = Enumerable.Empty<T>();
                return false;
            }

            instances = arrayList.Cast<T>();
            return true;
        }

        /// <summary>
        ///     Adds a listener that gets called when an instance of type T is registered. The listener will be called
        ///     immediately for each instance of type T that has already been registered.
        /// </summary>
        /// <param name="onRegistered">Action called whenever an instance is registered</param>
        /// <typeparam name="T">The type of instance registration to listen for</typeparam>
        public void AddOnRegisteredListener<T>(Action<T> onRegistered)
        {
            Type type = typeof(T);

            if (!m_instanceRegisteredActions.TryGetValue(type, out object actionListObject))
            {
                actionListObject = new List<Action<T>>();
                m_instanceRegisteredActions[type] = actionListObject;
            }

            List<Action<T>> list = (List<Action<T>>)actionListObject;
            list.Add(onRegistered);

            if (!m_registeredInstances.TryGetValue(type, out ArrayList instances)) return;

            //Invoke the listener for each instance of type already registered
            for (int i = 0; i < instances.Count; i++)
            {
                object instance = instances[i];

                // If it's a Unity object that's been destroyed, remove it.
                if (instance is Object unityObject && !unityObject)
                {
                    Debug.LogWarning($"InstanceLocator: Found instance of type {type} has been destroyed." +
                                     " Instances should unregistered themselves when destroyed.");
                    instances.RemoveAt(i--);
                    continue;
                }

                // Cast the instance to the correct type
                if (instance is not T typedInstance)
                {
                    // Somehow it's not the right type? Remove it.
                    instances.RemoveAt(i--);
                    continue;
                }

                onRegistered(typedInstance);
            }
        }

        public void RemoveOnRegisteredListener<T>(Action<T> onRegistered)
        {
            if (!m_instanceRegisteredActions.TryGetValue(typeof(T), out object actionListObject)) return;
            
            List<Action<T>> actionList = (List<Action<T>>)actionListObject;
            actionList.Remove(onRegistered);
        }

        /// <summary>
        ///     Adds a listener that gets called when an instance of type T is unregistered.
        /// </summary>
        /// <param name="onUnregistered">Action called whenever an instance is unregistered</param>
        /// <typeparam name="T">The type of instance unregistration to listen for</typeparam>
        public void AddOnUnregisteredListener<T>(Action<T> onUnregistered)
        {
            Type type = typeof(T);

            if (!m_instanceUnregisteredActions.TryGetValue(type, out object actionListObject))
            {
                actionListObject = new List<Action<T>>();
                m_instanceUnregisteredActions[type] = actionListObject;
            }

            List<Action<T>> list = (List<Action<T>>)actionListObject;
            list.Add(onUnregistered);
        }

        public void RemoveOnUnregisteredListener<T>(Action<T> onRegistered)
        {
            if (!m_instanceUnregisteredActions.TryGetValue(typeof(T), out object actionListObject)) return;
            
            List<Action<T>> actionList = (List<Action<T>>)actionListObject;
            actionList.Remove(onRegistered);
        }

        /// <summary>
        ///     Invokes all actions registered for the type using the argument provided.
        /// </summary>
        /// <param name="registeredActions">The dictionary containing the registered actions</param>
        /// <param name="item">The argument to invoke the actions with</param>
        /// <typeparam name="T">The type the invoke actions for</typeparam>
        private static void InvokeActions<T>(Dictionary<Type, object> registeredActions, T item)
        {
            if (!registeredActions.TryGetValue(typeof(T), out object actionListObject)) return;

            List<Action<T>> actionList = (List<Action<T>>)actionListObject;
            foreach (Action<T> action in actionList) action(item);
        }
    }
}