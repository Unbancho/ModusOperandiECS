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
            CurrentScene = scene;
        }

        public static ComponentManager<T> GetComponentManager<T>()
        {
            var componentManager = PerType<T>.ComponentManager;
            if (componentManager != null) return componentManager;
            SetComponentManager(new ComponentManager<T>());
            componentManager = PerType<T>.ComponentManager;

            return componentManager;
        }

        private static void SetComponentManager<T>(ComponentManager<T> componentManager)
        {
            PerType<T>.ComponentManager = componentManager;
        }

        private static class PerType<T>
        {
            public static ComponentManager<T> ComponentManager;
        }
    }
}