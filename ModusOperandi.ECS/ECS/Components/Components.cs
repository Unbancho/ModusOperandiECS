using System;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        Light
    }

    public class EntityMapFast : IEntityMap
    {
        private readonly uint[] _map = new uint[Ecs.MaxEntities];
        public uint this[Entity entity]
        {
            get => _map[entity];
            set => _map[entity] = value;
        }
    }

    public class EntityMapLight : IEntityMap
    {
        private readonly Dictionary<Entity, uint> _map = new();
        public uint this[Entity entity]
        {
            get => _map.TryGetValue(entity, out var idx) ? idx : 0;
            set => _map[entity] = value;
        }
    }

    [PublicAPI]
    public interface IComponentManager
    {
        internal static int Counter { get; set; }
        uint LookUp(Entity entity);
        Entity ReverseLookUp(uint index);
        public Entity[] EntitiesWithComponent { get; }
        public uint AssignedComponents { get;}
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
    
    
    public readonly ref struct Components<T> where T :
#if UNMANAGED
        unmanaged
#else
        struct
#endif
    {
        public readonly Span<T> ComponentArray;
        private readonly IEntityMap _map;
            
        public Components(Span<T> arr, IEntityMap map, int nComponents)
        {
            _map = map;
            ComponentArray = arr.Slice(1, nComponents);
        }

        public ref T Get(Entity e) => ref ComponentArray[(int)_map[e]];
    }
    
    
    [PublicAPI]
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    public class ComponentManager<T> : IComponentManager<T> where T :
#if UNMANAGED
        unmanaged
#else
        struct
#endif
    {
        private IEntityMap _map;
        private readonly List<Entity> _reverseMap = new() {0};

        public ref T GetComponent(Entity entity) => ref _managedComponents[LookUp(entity)];
        public Components<T> Components => new (_managedComponents, _map, (int)AssignedComponents);
        private T[] _managedComponents;
        object[] IComponentManager.Components => _managedComponents.Cast<object>().ToArray();
        public uint AssignedComponents { get; private set; }

        public void AddComponent(T component, Entity entity)
        {
            AssignedComponents++;
            _map[entity] = AssignedComponents;
            _reverseMap.Add(entity);
            if (_map[entity] >= _managedComponents.Length)
            {
                var managedComponents = _managedComponents;
                Array.Resize(ref managedComponents, _managedComponents.Length*2);
                _managedComponents = managedComponents;
            }
            _managedComponents[_map[entity]] = component;
        }

        public uint LookUp(Entity entity) => _map[entity];

        public Entity ReverseLookUp(uint index) => _reverseMap[(int)index];

        public Entity[] EntitiesWithComponent => _reverseMap.ToArray();

        public ComponentManager(ComponentStorageOptions options = ComponentStorageOptions.Fast)
        {
            _managedComponents = new T[1];
            _map = options switch
            {
                ComponentStorageOptions.Fast => new EntityMapFast(),
                ComponentStorageOptions.Light => new EntityMapLight(),
                _ => throw new ArgumentOutOfRangeException(nameof(options), options, null)
            };
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