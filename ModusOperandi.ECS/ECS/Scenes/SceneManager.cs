using ModusOperandi.ECS.Components;

namespace ModusOperandi.ECS.Scenes
{
    public static class SceneManager
    {
        public static Scene CurrentScene { get; set; }

        public static void SwitchScene(Scene scene)
        {
            CurrentScene = scene;
        }

        public static ComponentManager<T> GetComponentManager<T>() where T : IComponent
        {
            var componentManager = PerType<T>.ComponentManager;
            if (componentManager != null) return componentManager;
            SetComponentManager(new ComponentManager<T>());
            componentManager = PerType<T>.ComponentManager;

            return componentManager;
        }

        private static void SetComponentManager<T>(ComponentManager<T> componentManager) where T : IComponent
        {
            PerType<T>.ComponentManager = componentManager;
        }

        private static class PerType<T> where T : IComponent
        {
            public static ComponentManager<T> ComponentManager;
        }
    }
}