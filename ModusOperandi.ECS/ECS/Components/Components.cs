using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using ModusOperandi.ECS.Entities;

namespace ModusOperandi.ECS.Components
{
    internal static class SignatureCounter
    {
        public static int Counter;
    }

    [PublicAPI]
    public class ComponentManager<T> where T : unmanaged
    {
        public readonly int Index;

        // ReSharper disable once StaticMemberInGenericType
        public static ulong Signature { get; private set; }

        private readonly Dictionary<uint, uint> _map = new Dictionary<uint, uint>();
        private readonly List<uint> _reverseMap = new List<uint>{0};

        public ComponentManager()
        {
            Index = ++SignatureCounter.Counter;
            Signature = (ulong) 1 << Index;
            ManagedComponents = new T[1];
        }

        public T[] ManagedComponents;

        public uint AssignedComponents;

        public void AddComponent(T component, uint entity)
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

        public uint LookUp(uint entity) => _map.GetValueOrDefault<uint, uint>(entity, 0);
        
        public uint ReverseLookUp(uint index) => _reverseMap[(int)index];
        
        public ref T GetComponent(Entity entity) => ref GetComponent(entity.ID);

        public ref T GetComponent(uint entity) => ref ManagedComponents[LookUp(entity)];

        public enum SortOption
        {
            Insertion,
            [Obsolete]
            Quick
        }
        
        public void SortComponents(IComparer<T> comparer, SortOption sortOption = SortOption.Insertion)
        {
            Span<T> componentArray = ManagedComponents;
            componentArray = componentArray.Slice(1, (int)AssignedComponents);
            switch (sortOption)
            {
                case SortOption.Insertion:
                    InsertionSort(componentArray, comparer);
                    break;
                case SortOption.Quick:
                    QuickSort(componentArray, comparer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sortOption), sortOption, null);
            }
        }
        
        private void InsertionSort(Span<T> arr, IComparer<T> helper)
        {
            for (var j = 0; j < arr.Length-1; j++) {
                for (var i = 0; i < arr.Length-1; i++)
                {
                    if (helper.Compare(arr[i], arr[i+1]) < 0) continue;
                    SwapComponents(i+1, i+2);
                }
            }
        }
        
        // TODO: Make this actually quick I guess, Insertion rules for now.
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

        private void SwapComponents(int idxA, int idxB)
        {
            Span<T> arr = ManagedComponents;
            (arr[idxA], arr[idxB]) = (arr[idxB], arr[idxA]);
            var entityA = _reverseMap[idxA];
            var entityB = _reverseMap[idxB];
            (_map[entityA], _map[entityB]) = (_map[entityB], _map[entityA]);
            (_reverseMap[idxA], _reverseMap[idxB]) = (entityB, entityA);
        }
    }

    public class Component : Attribute
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
*/