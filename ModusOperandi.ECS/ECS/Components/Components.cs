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

    public interface IComponentManager
    {
        internal static int Counter { get; set; }
        uint LookUp(Entity entity);
        Entity ReverseLookUp(uint index);
        public Entity[] EntitiesWithComponent { get; }
        public uint AssignedComponents { get;}
        public object[] ManagedComponents { get; }
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
        public new T[] ManagedComponents { get; }
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

        public ref T GetComponent(Entity entity) => ref ManagedComponents[LookUp(entity)];
        public T[] ManagedComponents { get; private set; }
        object[] IComponentManager.ManagedComponents => ManagedComponents.Cast<object>().ToArray();
        public uint AssignedComponents { get; private set; }

        public void AddComponent(T component, Entity entity)
        {
            AssignedComponents++;
            _map[entity] = AssignedComponents;
            _reverseMap.Add(entity);
            if (_map[entity] >= ManagedComponents.Length)
            {
                var managedComponents = ManagedComponents;
                Array.Resize(ref managedComponents, ManagedComponents.Length*2);
                ManagedComponents = managedComponents;
            }
            ManagedComponents[_map[entity]] = component;
        }

        public uint LookUp(Entity entity) => _map[entity];

        public Entity ReverseLookUp(uint index) => _reverseMap[(int)index];

        public Entity[] EntitiesWithComponent => _reverseMap.ToArray();

        public ComponentManager(ComponentStorageOptions options = ComponentStorageOptions.Fast)
        {
            ManagedComponents = new T[1];
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

        [Obsolete]
        public void SortComponents(Comparison<T> comparison)
        {
           var componentArray = ((Span<T>)ManagedComponents).Slice(1, (int) AssignedComponents);
           
        }
    }
    
/*
    [PublicAPI]
    [Obsolete]
    public struct ValueComponentManager<T> : IComponentManager<T> where T :
#if UNMANAGED
        unmanaged
#else
        struct
#endif
    {
        public bool Initialized;
        
        public uint LookUp(Entity entity) => entity;
        public Entity ReverseLookUp(uint index) => index;

        private unsafe fixed uint _entitiesWithComponent[(int)Ecs.MaxEntities];
        public unsafe Entity[] EntitiesWithComponent
        {
            get
            {
                var arr = new Entity[AssignedComponents];
                for (var i = 0; i < AssignedComponents; i++)
                {
                    arr[i] = _entitiesWithComponent[i];
                }

                return arr;
            }
        }
        
        public uint AssignedComponents { get; private set; }
        public unsafe void AddComponent(T component, Entity entity)
        {
            AssignedComponents++;
            _managedComponents[entity] = (int) &component;
        }

        public unsafe ref T GetComponent(Entity entity)
        {
            return ref *(T*) _managedComponents[entity];
        }

        private unsafe fixed int _managedComponents[(int)Ecs.MaxEntities];
        public unsafe T[] ManagedComponents
        {
            get
            {
                var arr = new T[AssignedComponents];
                var a = AssignedComponents;
                for (var i = 1; i < a; i++)
                {
                    var p = (T*) _managedComponents[i];
                    if (p == default)
                    {
                        a++;
                        continue;
                    }
                    arr[i] = *p;
                }
                return arr;
            }
        }

        object[] IComponentManager.ManagedComponents => ManagedComponents.Cast<object>().ToArray();
    
        static ValueComponentManager()
        {
            IComponentManager<T>.Signature = 1u << ++SignatureCounter.Counter;
        }
        
        
        [Obsolete]
        public void SortComponents(Comparison<T> comparison)
        {
            var componentArray = ((Span<T>)ManagedComponents).Slice(1, (int) AssignedComponents);
           
        }
    }
*/

    [AttributeUsage(AttributeTargets.Struct)]
    public class Component : Attribute
    {
    }

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
    }
    
    internal static class SortHelper<T> where T: struct
    {
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