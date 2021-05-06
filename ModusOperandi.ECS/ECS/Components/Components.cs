using System;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ModusOperandi.ECS.Entities;



namespace ModusOperandi.ECS.Components
{
    public interface IEntityMap
    {
        uint this[Entity entity] { get; set; }
    }
    
    public enum ComponentStorageOptions
    {
        Fast,
        Light,
    }

    public struct EntityMapFast : IEntityMap
    {
        private uint[] _map;
        public uint this[Entity entity]
        {
            get => _map[entity];
            set
            {
                if(entity >= _map.Length)
                    Array.Resize(ref _map, (int)entity.ID+1);
                _map[entity] = value;
            }
        }

        public EntityMapFast(int capacity = 0)
        {
            _map = new uint[capacity];
        }

        public void Pop()
        {
            Array.Resize(ref _map, _map.Length-1);
        }
    }

    public readonly struct EntityMapLight : IEntityMap
    {
        private readonly Dictionary<Entity, uint> _map;
        public uint this[Entity entity]
        {
            get => _map.TryGetValue(entity, out var idx) ? idx : 0;
            set => _map[entity] = value;
        }

        public EntityMapLight(int i = 0)
        {
            _map = new();
        }
    }
    
    [PublicAPI]
    public interface IComponentManager
    {
        public static Type[] ComponentSignatures { get; } = new Type[Ecs.MaxComponents];
        uint LookUp(Entity entity);
        Entity ReverseLookUp(uint index);
        public Entity[] EntitiesWithComponent { get; }
        public int AssignedComponents { get;}
        public object[] Components { get; }
    }
    
    [PublicAPI]
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    public interface IComponentManager<T> : IComponentManager where T :
#if UNMANAGED
        unmanaged
#else
        struct
#endif
    {
        void AddComponent(T component, Entity entity);
        ref T GetComponent(Entity entity);
        public new Components<T> Components { get; } 
    }

    [PublicAPI]
    public readonly ref struct Components<T> where T :
#if UNMANAGED
        unmanaged
#else
        struct
#endif
    {
        public Span<T> ComponentArray => _componentArray.Slice(1, _componentArray.Length - 1);
        private readonly Span<T> _componentArray;
        private readonly IEntityMap _map;
            
        public Components(Span<T> arr, IEntityMap map)
        {
            _map = map;
            _componentArray = arr;
        }
        
        public Components(T[] arr, IEntityMap map, int nComponents)
        {
            _map = map;
            _componentArray = arr;
            _componentArray = _componentArray.Slice(0, nComponents + 1);
        }

        public ref T Get(Entity e) => ref _componentArray[(int)_map[e]];
    }
    
    public class ComponentManager<T> : IComponentManager<T> where T : unmanaged
    {
        private T[] _components;
        public uint LookUp(Entity entity) => _map[entity];
        public Entity ReverseLookUp(uint index) => _reverseMap[(int)index];
        public Entity[] EntitiesWithComponent => _reverseMap.ToArray();

        public int AssignedComponents { get; private set; }
        public void AddComponent(T component, Entity entity)
        {
            AssignedComponents++;
            if (entity.ID >= _components.Length)
            {
                Array.Resize(ref _components, _components.Length+50);
            }
            _map[entity] = (uint) AssignedComponents;
            _reverseMap.Add(entity);
            _components[_map[entity]] = component;
        }

        public void RemoveComponent(Entity entity)
        {
            Swap((int)_map[entity], AssignedComponents);
            AssignedComponents--;
            _reverseMap.RemoveAt(_reverseMap.Count-1);
            _map.Pop();
        }

        public void Swap(Entity left, Entity right)
        {
            (_components[_map[left]], _components[_map[right]]) = (_components[_map[right]], _components[_map[left]]);
            (_reverseMap[(int) _map[left]], _reverseMap[(int) _map[right]]) =
                (_reverseMap[(int) _map[right]], _reverseMap[(int) _map[left]]);
            (_map[left], _map[right]) = (_map[right], _map[left]);
        }

        public void Swap(int left, int right)
        {
            (_components[right], _components[left]) = (_components[left], _components[right]);
            (_map[_reverseMap[left]], _map[_reverseMap[right]]) = (_map[_reverseMap[right]], _map[_reverseMap[left]]);
            (_reverseMap[left], _reverseMap[right]) = (_reverseMap[right], _reverseMap[left]);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent(Entity entity)
        {
            return ref _components[_map[entity]];
        }

        private EntityMapFast _map;
        private readonly List<Entity> _reverseMap;

        public Components<T> Components => new (_components, _map, AssignedComponents);
        

        object[] IComponentManager.Components => Array.Empty<object>();

        public ComponentManager(int capacity=50)
        {
            _components = new T[capacity];
            AssignedComponents = 0;
            _map = new(1);
            _reverseMap = new() {0};
        }
    }

    [AttributeUsage(AttributeTargets.Struct)]
    public class Component : Attribute
    {
    }

    [PublicAPI]
    public static class ComponentExtensions
    {
        public static T[] GetComponents<T>(this IComponentManager<T> manager, params Entity[] entities) where T :
#if UNMANAGED
            unmanaged
#else
            struct
#endif
        {
            var components = new T[entities.Length];
            for (var i = 0; i < entities.Length; i++)
            {
                components[i] = manager.GetComponent(entities[i]);
            }

            return components;
        }
    }
}
