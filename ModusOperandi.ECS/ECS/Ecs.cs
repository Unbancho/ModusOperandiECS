using System.Collections.Generic;
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
        public static ulong MaxEntities = 1_000_000;
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

        public static Entity[] Query(Archetype archetype)
        {
            var entities = new List<Entity>();
            var indices = GetComponentIndices(archetype.Signature);
            for (var entity = 0u; entity < MaxEntities; entity++)
            {
                if (EntityMatches(entity, indices)) entities.Add(entity);
            }

            return entities.ToArray();
        }
        
        // TODO: Change loop to iterate a collection of Entities.
        public static Entity[] Query(Archetype archetype, Scene scene)
        {
            var entities = new List<Entity>();
            var indices = GetComponentIndices(archetype.Signature);
            for (var entity = 0u; entity < scene.EntityManager.NumberOfAliveEntities; entity++)
            {
                if (EntityMatches(entity, indices)) entities.Add(entity);
            }

            return entities.ToArray();
        }
        
        private static bool EntityMatches(uint entity, int[] indices)
        {
            for (var i = 0; i < indices.Length; i++)
            {
                if (!EntityHasComponent(entity, indices[i])) return false;
            }
            return true;
        }
        
        private static Dictionary<ulong, int[]> _signatureIndices = new Dictionary<ulong, int[]>();
        private static int[] GetComponentIndices(ulong signature)
        {
            if (_signatureIndices.TryGetValue(signature, out var idxs)) return idxs;
            var indices = new List<int>();
            for (var i = 0; signature >= 1u << i; i++)
                if ((signature & (1u << i)) > 0)
                    indices.Add(i);
            var arr = indices.ToArray();
            _signatureIndices[signature] = arr;
            return arr;
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