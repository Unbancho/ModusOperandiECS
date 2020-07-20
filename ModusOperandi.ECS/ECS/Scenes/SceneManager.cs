using JetBrains.Annotations;
using ModusOperandi.ECS.Components;

namespace ModusOperandi.ECS.Scenes
{
    [PublicAPI]
    public static class SceneManager
    {
        public static Scene CurrentScene { get; set; }

        public static void SwitchScene(Scene scene)
        {
            if(CurrentScene != null)
                foreach (dynamic system in CurrentScene.GetAllSystems())
                {
                    CurrentScene.StopSystem(system);
                }
            CurrentScene = scene;
        }

        public static ComponentManager<T> GetComponentManager<T>() where T : unmanaged
        {
            var componentManager = PerType<T>.ComponentManager;
            if (componentManager != null) return componentManager;
            SetComponentManager(new ComponentManager<T>());
            componentManager = PerType<T>.ComponentManager;

            return componentManager;
        }

        private static void SetComponentManager<T>(ComponentManager<T> componentManager) where T: unmanaged
        {
            PerType<T>.ComponentManager = componentManager;
        }

        private static class PerType<T> where T: unmanaged
        {
            public static ComponentManager<T> ComponentManager;
        }
    }
}