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
        internal static int Counter { get; set; }
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
        static int Index { get; protected set; }
        static ulong Signature => 1u << Index;
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

        public ref T Get(Entity e) => ref _componentArray[(int)_map[e]];
    }
    
    public unsafe class ComponentManager<T> : IComponentManager<T> where T : unmanaged
    {
        private T* _components;

        public uint LookUp(Entity entity)
        {
            return _map[entity];
        }

        public Entity ReverseLookUp(uint index)
        {
            return _reverseMap[(int)index];
        }

        public Entity[] EntitiesWithComponent => _reverseMap.ToArray();

        private readonly int _size;
        private int _capacity;
        public int Capacity
        {
            get => _capacity;
            set
            {
                _components = (T*) Marshal.ReAllocHGlobal(new (_components), new ((value+1)*_size));
                _capacity = value;
            }
        }

        public int AssignedComponents { get; private set; }
        public void AddComponent(T component, Entity entity)
        {
            if (AssignedComponents == Capacity)
                Capacity += 10;
            AssignedComponents++;
            _map[entity] = (uint) AssignedComponents;
            _reverseMap.Add(entity);
            _components[_map[entity]] = component;
        }

        public ref T GetComponent(Entity entity)
        {
            return ref _components[_map[entity]];
        }

        private EntityMapFast _map;
        private readonly List<Entity> _reverseMap;

        public Components<T> Components
            => new (new (_components, AssignedComponents), _map);
        

        object[] IComponentManager.Components => Array.Empty<object>();

        public ComponentManager(int capacity=50)
        {
            _size = sizeof(T);
            _capacity = capacity;
            _components = (T*) Marshal.AllocHGlobal(_size * capacity).ToPointer();
            AssignedComponents = 0;
            _map = new(1);
            _reverseMap = new() {0};
        }
        
        static ComponentManager()
        {
            IComponentManager<T>.Index = IComponentManager.Counter++;
        }
    }

    [AttributeUsage(AttributeTargets.Struct)]
    public class Component : Attribute
    {
    }

    [PublicAPI]
    public static class ComponentExtensions
    {
        public static ulong GetComponentSignature<T>(this T _) where T :
#if UNMANAGED
            unmanaged
#else
            struct
#endif
        {
            return IComponentManager<T>.Signature;
        }
        
        public static int GetComponentIndex<T>(this T _) where T :
#if UNMANAGED
            unmanaged
#else
            struct
#endif
        {
            return IComponentManager<T>.Index;
        }
        
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



/*
private void Destroy(uint i)
{
    var last = NumberOfInstances - 1;
    var entity = Entities[i];
    var lastEntity = Entities[last];
    Entities[i] = Entities[last];
    ManagedComponents[i] = ManagedComponents[last];
    _map[lastEntity] = i;
    _map.Remove(entity);
    NumberOfInstances--;
}

        [Obsolete]
        private void QuickSort(Span<T> array, IComparer<T> helper)
        {
            int Partition(Span<T> arr)
            {
                ref var pivot = ref arr[^1];
                var i = -1;
                for (var j = 0; j < arr.Length-1; j++)
                {
                    if (helper.Compare(arr[j], pivot) > 0) continue;
                    i++;
                    SwapComponents(i+1, j+1);
                }
                SwapComponents(i+2, arr.Length);
                return i+1;
            }
            
            while (true)
            {
                if (array.Length <= 1) return;
                var q = Partition(array);
                array = array[..q++];
                QuickSort(array, helper);
                if (q >= array.Length - 1) continue;
                array = array[q..];
                QuickSort(array, helper);
            }
        }
*/