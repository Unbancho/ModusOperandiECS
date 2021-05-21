using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using ModusOperandi.ECS.Entities;

namespace ModusOperandi.ECS.Components
{
    [PublicAPI]
    public interface IComponentManager
    {
        int LookUp(Entity entity);
        Entity ReverseLookUp(uint index);
        public Entity[] EntitiesWithComponent { get; }
        public int AssignedComponents { get;}
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
        public Components<T> Components { get; } 
    }
    
    public readonly unsafe struct Components<T> where T: unmanaged
    {
        private readonly int* _map;
        public readonly int Count;

        public Components(int* map, T* components, int count)
        {
            _map = map;
            ComponentsPointer = components;
            Count = count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(uint i) => ref ComponentsPointer[_map[i]];
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(Entity e) => ref ComponentsPointer[_map[e.Index]];
        public Span<T> ComponentsSpan => new(ComponentsPointer+1, Count);
        public T* ComponentsPointer { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LookUp(Entity e) => _map[e.Index];
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
            _map[entity.Index] = AssignedComponents;
            _reverseMap.Add(entity.Index);
            var arr = (T*)ComponentArrays.Arrays[_index];
            arr[_map[entity.Index]] = component;
        }

        public unsafe void RemoveComponent(Entity entity)
        {
            Swap(_map[entity.Index], AssignedComponents);
            AssignedComponents--;
            _reverseMap.RemoveAt(_reverseMap.Count-1);
            //_map.Pop();
        }

        public unsafe void Swap(Entity left, Entity right)
        {
            (_components[_map[left.Index]], _components[_map[right.Index]]) = 
                (_components[_map[right.Index]], _components[_map[left.Index]]);
            (_reverseMap[_map[left.Index]], _reverseMap[_map[right.Index]]) =
                (_reverseMap[_map[right.Index]], _reverseMap[_map[left.Index]]);
            (_map[left.Index], _map[right.Index]) = (_map[right.Index], _map[left.Index]);
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
            int GetIndex() => _map[entity.Index];
            static ref T IndexIntoArray(T* arr, int idx) => ref arr[idx]; 
            return ref IndexIntoArray(GetArray(),GetIndex());
        }
        
        private readonly unsafe int* _map;
        private readonly List<Entity> _reverseMap;

        public unsafe Components<T> Components => new(_map, (T*) ComponentArrays.Arrays[_index],
            AssignedComponents);
        

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
