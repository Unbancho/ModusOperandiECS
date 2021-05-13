using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using ModusOperandi.ECS.Entities;

namespace ModusOperandi.ECS.Components
{
    [PublicAPI]
    public interface IComponentManager
    {
        public static Type[] ComponentSignatures { get; } = new Type[Ecs.MaxComponents];
        int LookUp(Entity entity);
        Entity ReverseLookUp(uint index);
        public Entity[] EntitiesWithComponent { get; }
        public int AssignedComponents { get;}
        public unsafe void* Components { get; }
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
            
        public Components(Span<T> arr)
        {
            _componentArray = arr;
        }
        
        public Components(T[] arr, int nComponents)
        {
            _componentArray = arr;
            _componentArray = _componentArray.Slice(0, nComponents + 1);
        }
    }

    public static unsafe class ComponentArrays
    {
        public static readonly void** Arrays = (void**) Marshal.AllocHGlobal(sizeof(void*)*Ecs.MaxComponents).ToPointer();
    }
    
    public class ComponentManager<T> : IComponentManager<T> where T : unmanaged
    {
        private readonly int _index;
        private unsafe Span<T> _components => new (ComponentArrays.Arrays[_index], AssignedComponents+1);
        public unsafe int LookUp(Entity entity) => _map[entity];
        public Entity ReverseLookUp(uint index) => _reverseMap[(int)index];
        public Entity[] EntitiesWithComponent => _reverseMap.ToArray();

        private int _capacity;
        public int AssignedComponents { get; private set; }
        public unsafe void AddComponent(T component, Entity entity)
        {
            AssignedComponents++;

            if (_capacity <= AssignedComponents)
            {
                ComponentArrays.Arrays[_index] = (T*) Marshal.ReAllocHGlobal(
                    new (ComponentArrays.Arrays[_index]),
                    new ((AssignedComponents+1) * sizeof(T))).ToPointer();
                _capacity = AssignedComponents-1;
            }
            _map[entity] = AssignedComponents;
            _reverseMap.Add(entity);
            var arr = (T*)ComponentArrays.Arrays[_index];
            arr[_map[entity]] = component;
        }

        public unsafe void RemoveComponent(Entity entity)
        {
            Swap(_map[entity], AssignedComponents);
            AssignedComponents--;
            _reverseMap.RemoveAt(_reverseMap.Count-1);
            //_map.Pop();
        }

        public unsafe void Swap(Entity left, Entity right)
        {
            (_components[_map[left]], _components[_map[right]]) = 
                (_components[_map[right]], _components[_map[left]]);
            (_reverseMap[_map[left]], _reverseMap[_map[right]]) =
                (_reverseMap[_map[right]], _reverseMap[_map[left]]);
            (_map[left], _map[right]) = (_map[right], _map[left]);
        }

        public unsafe void Swap(int left, int right)
        {
            (_components[right], _components[left]) = (_components[left], _components[right]);
            (_map[_reverseMap[left]], _map[_reverseMap[right]]) = (_map[_reverseMap[right]], _map[_reverseMap[left]]);
            (_reverseMap[left], _reverseMap[right]) = (_reverseMap[right], _reverseMap[left]);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref T GetComponent(Entity entity)
        {
            T* GetArray() => (T*) ComponentArrays.Arrays[_index];
            int GetIndex() => _map[entity];
            static ref T IndexIntoArray(T* arr, int idx) => ref arr[idx]; 
            return ref IndexIntoArray(GetArray(),GetIndex());
        }
        
        private readonly unsafe int* _map;
        private readonly List<Entity> _reverseMap;

        public Components<T> Components => new (_components);
        

        unsafe void* IComponentManager.Components => ComponentArrays.Arrays[_index];

        public unsafe ComponentManager(int capacity=50)
        {
            _index = Ecs.GetIndex<T>();
            _capacity = capacity;
            ComponentArrays.Arrays[_index] = (T*) Marshal.AllocHGlobal(sizeof(T)*_capacity).ToPointer();
            _map = (int*) Marshal.AllocHGlobal(new IntPtr((1u<<20)-1 * sizeof(int))).ToPointer();
            AssignedComponents = 0;
            _reverseMap = new() {0};
        }
    }

    [AttributeUsage(AttributeTargets.Struct)]
    public class Component : Attribute
    {
    }
}
