using System;
using System.Runtime.InteropServices;
using ModusOperandi.ECS.Entities;

namespace ModusOperandi.ECS.Components
{
    public static unsafe class ComponentsUnsafe
    {
        private struct MapStruct
        {
            public const int Max = 10_000;
            public fixed int Map[Max];
            public fixed uint ReverseMap[Max];
            public int Count;
        }

        private static readonly MapStruct[] MapArrays = new MapStruct[Ecs.MaxComponents];
        
        private static readonly void*[] ComponentArrays = new void*[Ecs.MaxComponents];

        public static void AddComponent<T>(Entity e, T component) where  T: unmanaged
        {
            var array = (T*) ComponentArrays[Ecs.GetIndex<T>()];
            var map = MapArrays[Ecs.GetIndex<T>()];
            map.Map[e] = map.Count;
            map.Count++;
            array[map.Count] = component;
            map.ReverseMap[map.Count] = e;
        }
        
        public static ref T Get<T>(int i) where  T: unmanaged
        {
            return ref ((T*) ComponentArrays[Ecs.GetIndex<T>()])[i];
        }
        
        public static ref T Get<T>(Entity e) where  T: unmanaged
        {
            return ref ((T*) ComponentArrays[Ecs.GetIndex<T>()])[GetMapping<T>(e)];
        }

        public static int GetMapping<T>(Entity e) where  T: unmanaged
        {
            return MapArrays[Ecs.GetIndex<T>()].Map[(int)(uint) e];
        }
        
        public static Entity GetReverseMapping<T>(int i) where  T: unmanaged
        {
            return MapArrays[Ecs.GetIndex<T>()].ReverseMap[i];
        }

        public static Span<T> GetComponents<T>() where  T: unmanaged
        {
            return new(ComponentArrays[Ecs.GetIndex<T>()], 
                MapArrays[Ecs.GetIndex<T>()].Count);
        }

        static ComponentsUnsafe()
        {
            for (var i = 0; i < ComponentArrays.Length; i++)
            {
                ComponentArrays[i] = Marshal.AllocHGlobal(100 * 48).ToPointer();
            }
        }
    }

    public static class Extends
    {
        public static ulong Signature<T>(this T c) where  T: unmanaged
        {
            return Ecs.GetSignature<T>();
        }
        
        public static int Index<T>(this T c) where  T: unmanaged
        {
            return Ecs.GetIndex<T>();
        }
    }
}