using System;
using JetBrains.Annotations;
using ModusOperandi.ECS.Components;

namespace ModusOperandi.ECS.Archetypes
{

    public interface IArchetype
    {
        public ulong Signature { get; }
        public ulong AntiSignature { get;}
        public int[] Indices { get; }
        public int[] AntiIndices { get; }
    }
    
    public readonly struct Archetype : IArchetype
    {
        public ulong Signature { get; }
        public ulong AntiSignature { get; }
        
        public int[] Indices { get; }
        public int[] AntiIndices { get; }

        public Archetype(ulong signature, ulong antiSignature, int[] indices, int[] antiIndices)
        {
            Signature = signature;
            AntiSignature = antiSignature;
            Indices = indices;
            AntiIndices = antiIndices;
        }
    }

    [PublicAPI]
    public class Archetype<T> : IArchetype where T :
#if UNMANAGED
        unmanaged
#else
        struct
#endif
    {
        public ulong Signature { get; private set; } = IComponentManager<T>.Signature;
        public ulong AntiSignature { get; private set; }

        public int[] Indices => CalculateIndices(Signature);

        public int[] AntiIndices => CalculateIndices(AntiSignature);

        private static int[] CalculateIndices(ulong sig)
        {
            if (sig == 0) return Array.Empty<int>();
            Span<int> indices = stackalloc int[(int)Math.Log2(sig)+1];
            var counter = 0;
            for (var i = 0; 1u << i <= sig; i++)
            {
                if(((1u << i) & sig) == 0) continue;
                indices[counter++] = i;
            }

            return indices.Slice(0, counter).ToArray();
        }

        public class Include<TI> : Archetype<T> where TI :
#if UNMANAGED
            unmanaged
#else 
            struct
#endif
        {
            public Include()
            {
                Signature |= IComponentManager<TI>.Signature;
            }
        }
        
        public class Exclude<TE> : Archetype<T> where TE :
#if UNMANAGED
            unmanaged
#else 
            struct
#endif
        {
            public Exclude()
            {
                AntiSignature |= IComponentManager<TE>.Signature;
            }
        }
        
        public static implicit operator Archetype(Archetype<T> a) 
            => new(a.Signature, a.AntiSignature, a.Indices, a.AntiIndices);
    }
}