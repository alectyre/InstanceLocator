using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Architecture
{
    public class InstanceLocator : MonoBehaviour
    {
        private readonly InstanceContainer m_instanceContainer = new();

        private static InstanceContainer m_global;
        private static Dictionary<Scene, InstanceContainer> m_sceneContainers;
        private static bool m_isQuitting;
        private static readonly InstanceContainer EmptyInstanceContainer = new();

        private const string GlobalServiceLocatorName = "ServiceLocator [Global]";
        private const string SceneServiceLocatorName = "ServiceLocator [Scene]";

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            Application.wantsToQuit += HandleApplicationWantsToQuit;
        }

        private static bool HandleApplicationWantsToQuit()
        {
            m_isQuitting = true;

            return true;
        }

        private void OnDestroy()
        {
            if (m_global == m_instanceContainer)
                m_global = null;
            else if (m_sceneContainers.ContainsValue(m_instanceContainer))
                m_sceneContainers.Remove(gameObject.scene);
        }

        /// <summary>
        ///     Returns the global ServiceLocator instance. If it doesn't exist, it will be created.
        /// </summary>
        /// <returns></returns>
        public static InstanceContainer Global
        {
            get
            {
                if (m_isQuitting) return EmptyInstanceContainer;

                if (m_global != null) return m_global;

                GameObject gameObject = new(GlobalServiceLocatorName);
                InstanceLocator instanceLocator = gameObject.AddComponent<InstanceLocator>();

                return ConfigureAsGlobal(instanceLocator, true);
            }
        }

        /// <summary>
        ///     Returns the ServiceLocator instance for the given scene. If it doesn't exist, it will be created.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public static InstanceContainer ForScene(Scene scene)
        {
            if (m_isQuitting) return EmptyInstanceContainer;

            if (m_sceneContainers.TryGetValue(scene, out InstanceContainer instanceContainer))
                return instanceContainer;

            GameObject gameObject = new(SceneServiceLocatorName);
            InstanceLocator instanceLocator = gameObject.AddComponent<InstanceLocator>();

            return ConfigureForScene(instanceLocator, scene);
        }

        /// <summary>
        ///     Returns the ServiceLocator instance for the given GameObject, 
        ///     searching for an instance in the GameObject's parent hierarchy.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static InstanceContainer For(GameObject gameObject)
        {
            if (m_isQuitting) return EmptyInstanceContainer;

            InstanceLocator instanceLocator = gameObject.GetComponentInParent<InstanceLocator>();
            return instanceLocator ? instanceLocator.m_instanceContainer : EmptyInstanceContainer;
        }

        /// <summary>
        ///     Returns the closest ServiceLocator instance for the given GameObject.
        ///     If no instance is registered for the GameObject, it will return the instance for the scene.
        ///     If no instance is registered for the scene, it will return the global instance.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static InstanceContainer ClosestFor(GameObject gameObject)
        {
            if (m_isQuitting) return EmptyInstanceContainer;

            return For(gameObject) ?? ForScene(gameObject.scene) ?? Global;
        }

        private static InstanceContainer ConfigureAsGlobal(InstanceLocator instanceLocator, bool dontDestroyOnLoad)
        {
            InstanceContainer instanceContainer = instanceLocator.m_instanceContainer;
            if (m_global == instanceContainer)
            {
                Debug.LogWarning("ServiceLocator.ConfigureAsGlobal: Already configured as global");
            }
            else if (m_global != null)
            {
                Debug.LogError(
                    "ServiceLocator.ConfigureAsGlobal: Another ServiceLocator" +
                    " is already configured as global");
            }
            else
            {
                m_global = instanceContainer;
                if (dontDestroyOnLoad) DontDestroyOnLoad(instanceLocator.gameObject);
            }

            return m_global;
        }

        private static InstanceContainer ConfigureForScene(InstanceLocator instanceLocator, Scene scene)
        {
            if (m_sceneContainers.TryGetValue(scene, out InstanceContainer instanceContainer))
            {
                Debug.LogError(
                    "ServiceLocator.ConfigureForScene: Another ServiceLocator " +
                    "is already configured for this scene");
                return instanceContainer;
            }
            
            SceneManager.MoveGameObjectToScene(instanceLocator.gameObject, scene);
            m_sceneContainers.Add(scene, instanceLocator.m_instanceContainer);
            return instanceLocator.m_instanceContainer;
        }
    }
}