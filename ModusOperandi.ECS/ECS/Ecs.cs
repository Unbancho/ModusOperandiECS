using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        public const ulong MaxEntities = 1_000_000;
        public const int MaxComponents = sizeof(ulong);
        private static ulong[] _entityArchetypes = Array.Empty<ulong>();


        //TODO: Implement.
        public static Entity CopyEntity(Entity e, Scene scene)
        {
            var e2 = scene.EntityManager.CreateEntity();
            for (var i = 0; i < MaxComponents; i++)
            {
            }

            return e2;
        }
        
        public static void RegisterComponent<T>(Entity e, T component) where T :
#if UNMANAGED
            unmanaged
#else 
            struct
#endif
        {
            GetComponentManager<T>().AddComponent(component, e);
            var sig = IComponentManager<T>.Signature;
            if(_entityArchetypes.Length <= e.Index)
                Array.Resize(ref _entityArchetypes, _entityArchetypes.Length*2+2);
            _entityArchetypes[e.Index] |= sig;
            foreach (var archetype in _dirtyDict.Keys)
            {
                if ((archetype.Signature & sig) != 0 && (archetype.AntiSignature & sig) == 0)
                    _dirtyDict[archetype] = true;
            }
        }

        public static void UnregisterComponent<T>(Entity e) where T:
#if UNMANAGED
        unmanaged
#else 
            struct
#endif
        {
            GetComponentManager<T>().RemoveComponent(e);
            var sig = IComponentManager<T>.Signature;
            _entityArchetypes[e.Index] &= ~sig;
            foreach (var archetype in _dirtyDict.Keys)
            {
                if ((archetype.Signature & sig) != 0 && (archetype.AntiSignature & sig) == 0) 
                    _dirtyDict[archetype] = true;
            }
        }

        public static void RegisterComponent(Entity e, object component)
        {
            RegisterComponent(e, (dynamic) component);
        }

        public static ulong GetEntityArchetype(Entity e) => _entityArchetypes[e];
        

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                var indices = a.Indices;
                var min = int.MaxValue;
                var idx = indices.Length - 1;
                for (var i = 0; i < indices.Length; i++)
                {
                    var n = _componentManagers[indices[i]].AssignedComponents;
                    if (min <= n) continue;
                    min = n;
                    idx = i;
                }
                var sceneEntities = s?.Entities ?? Array.Empty<Entity>();
                var entitiesWithRarest = _componentManagers[indices[idx]].EntitiesWithComponent;
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
        private static Dictionary<Archetype, bool> _dirtyDict = new();
        public static Span<Entity> Query(Archetype archetype, Entity[] entitiesToQuery)
        {
            if (!_dirtyDict.TryGetValue(archetype, out var dirty))
                (dirty, _dirtyDict[archetype]) = (true, true);
            if (!dirty && _queryCache.TryGetValue(archetype, out var cachedEntities))
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
            _dirtyDict[archetype] = false;
            return r;
        }

        [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
        [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
        public static bool EntityMatches(uint entity, Span<int> indices, Span<int> antiIndices)
        {
            var entityArchetype = _entityArchetypes[entity];
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