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

        public static ref T Get<T>(int i) where  T: unmanaged
        {
            return ref ((T*) ComponentArrays[IComponentManager<T>.Index])[i];
        }
        
        public static ref T Get<T>(Entity e) where  T: unmanaged
        {
            return ref ((T*) ComponentArrays[IComponentManager<T>.Index])[GetMapping<T>(e)];
        }

        public static int GetMapping<T>(Entity e) where  T: unmanaged
        {
            return MapArrays[IComponentManager<T>.Index].Map[(int)(uint) e];
        }
        
        public static Entity GetReverseMapping<T>(int i) where  T: unmanaged
        {
            return MapArrays[IComponentManager<T>.Index].ReverseMap[i];
        }

        public static Span<T> GetComponents<T>() where  T: unmanaged
        {
            return new(ComponentArrays[IComponentManager<T>.Index], 
                MapArrays[IComponentManager<T>.Index].Count);
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
        public static int Signature<T>(this T c) where  T: unmanaged
        {
            return IComponentManager<T>.Index;
        }
    }
}