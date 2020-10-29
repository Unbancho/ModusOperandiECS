using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using ModusOperandi.ECS.Archetypes;
using ModusOperandi.ECS.Components;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.Scenes;

namespace ModusOperandi.ECS
{

    [PublicAPI]
    public static class Ecs
    {
        public static ulong MaxEntities = 1024;
        public static readonly Dictionary<ulong, Entity[]> ArchetypeEntityDictionary = new Dictionary<ulong, Entity[]>();
        public static readonly ulong[] EntityArchetypes = new ulong[MaxEntities];

        public static ComponentManager<T> GetComponentManager<T>() where T : unmanaged
        {
            var componentManager = PerType<T>.ComponentManager;
            if (componentManager != null) return componentManager;
            SetComponentManager(new ComponentManager<T>());
            return PerType<T>.ComponentManager;
        }

        private static void SetComponentManager<T>(ComponentManager<T> componentManager) where T : unmanaged
        {
            PerType<T>.ComponentManager = componentManager;
        }

        private static class PerType<T> where T : unmanaged
        {
            public static ComponentManager<T> ComponentManager;
        }

        public static IEnumerable<Entity> Query(Archetype archetype)
        {
            for (uint i = 0; i < MaxEntities; i++)
                if (EntityMatches(i, GetComponentIndices(archetype.Signature)))
                    yield return i;
        }

        private static bool EntityMatches(uint entity, IEnumerable<int> indices)
        {
            return indices.All(index => EntityHasComponent(entity, index));
        }

        private static IEnumerable<int> GetComponentIndices(ulong signature)
        {
            for (var i = 0; signature >= 1u << i; i++)
                if ((signature & (1u << i)) > 0)
                    yield return i;
        }

        public static bool EntityHasComponent(uint entity, int componentIndex)
        {
            return (EntityArchetypes[entity] & 1u << componentIndex) != 0;
        }
    }

    [PublicAPI]
    public class RealSceneManager
    {
        public void ActivateScene(Scene scene)
        {
            if(ActiveScenes.Add(scene))
                scene.Initialize();
        }

        public void DeactivateScene(Scene scene)
        {
            ActiveScenes.Remove(scene);
        }

        public HashSet<Scene> ActiveScenes { get; } = new HashSet<Scene>();
    }
}