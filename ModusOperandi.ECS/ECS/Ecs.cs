using System;
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
        public const ulong MaxEntities = 10_000;
        private static readonly ulong[] EntityArchetypes = new ulong[MaxEntities];

        public static void RegisterComponent<T>(Entity e, T component) where T :
#if UNMANAGED
            unmanaged
#else 
            struct
#endif
        {
            GetComponentManager<T>().AddComponent(component, e);
            EntityArchetypes[e.Index] |= IComponentManager<T>.Signature;
            _dirty = true;
        }

        public static void RegisterComponent(Entity e, object component)
        {
            RegisterComponent(e, (dynamic) component);
        }

        public static ulong GetEntityArchetype(Entity e) => EntityArchetypes[e];
        

        public static ComponentManager<T> GetComponentManager<T>() where T :
#if UNMANAGED
            unmanaged
#else 
            struct
#endif
        {
            var componentManager = PerType<T>.ComponentManager;
            if (componentManager != null) return componentManager;
            SetComponentManager(new ComponentManager<T>());
            return PerType<T>.ComponentManager;
        }

        public static void SetComponentManager<T>(ComponentManager<T> componentManager) where T :
#if UNMANAGED
            unmanaged
#else 
            struct
#endif
        {
            PerType<T>.ComponentManager = componentManager;
            _componentManagers.Add(componentManager);
        }

        public static IComponentManager[] GetComponentManagers() => _componentManagers.ToArray();

        private static class PerType<T> where T :
#if UNMANAGED
            unmanaged
#else 
            struct
#endif
        {
            public static ComponentManager<T> ComponentManager;
        }
        
        
        private static List<IComponentManager> _componentManagers = new();
        public static Span<Entity> Query(Archetype archetype, Scene scene=null)
        {
            static Entity[] SmallestGroup(Archetype a, Scene s = null)
            {
                var sceneEntities = s?.Entities ?? Array.Empty<Entity>();
                var entitiesWithRarest = _componentManagers[a.Indices[^1]].EntitiesWithComponent;
                if (entitiesWithRarest.Length < sceneEntities.Length || sceneEntities.Length == 0)
                {
                    return entitiesWithRarest;
                }

                return sceneEntities;
            }

            var smallestGroup = SmallestGroup(archetype, scene);
            return Query(archetype, smallestGroup);
        }

        private static Dictionary<Archetype, Entity[]> _queryCache = new();
        private static bool _dirty;
        public static Span<Entity> Query(Archetype archetype, Entity[] entitiesToQuery)
        {
            if (!_dirty && _queryCache.TryGetValue(archetype, out var cachedEntities))
                return cachedEntities;
            Span<Entity> entities = new Entity[entitiesToQuery.Length];
            var filteredEntities = 0;
            foreach (var entity in entitiesToQuery)
            {
                if (EntityMatches(entity, archetype.Indices, archetype.AntiIndices))
                {
                    entities[filteredEntities++] = entity;
                }
            }

            var r = entities.Slice(0, filteredEntities);
            _queryCache[archetype] = r.ToArray();
            return r;
        }

        public static bool EntityMatches(uint entity, int[] indices, int[] antiIndices)
        {
            var entityArchetype = EntityArchetypes[entity];
            for (var i = 0; i < indices.Length; i++)
            {
                if ((entityArchetype & 1u << indices[i]) == 0) return false;
            }

            for (var i = 0; i < antiIndices.Length; i++)
            {
                if ((entityArchetype & 1u << antiIndices[i]) != 0) return false;
            }
            return true;
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

        public HashSet<Scene> ActiveScenes { get; } = new();
    }
}