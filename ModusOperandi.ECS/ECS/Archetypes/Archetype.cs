using System;
using JetBrains.Annotations;
using ModusOperandi.ECS.Components;

namespace ModusOperandi.ECS.Archetypes
{

    public interface IArchetype
    {
        public ulong Signature { get; }
        public ulong AntiSignature { get;}
        public Span<int> Indices { get; }
        public Span<int> AntiIndices { get; }
    }
    
    public readonly struct Archetype : IArchetype
    {
        public ulong Signature { get; }
        public ulong AntiSignature { get; }

        private readonly int[] _indices;
        public Span<int> Indices => _indices;
        private readonly int[] _antiIndices;
        public Span<int> AntiIndices => _antiIndices;

        public Archetype(ulong signature, ulong antiSignature, Span<int> indices, Span<int> antiIndices)
        {
            Signature = signature;
            AntiSignature = antiSignature;
            _indices = indices.ToArray();
            _antiIndices = antiIndices.ToArray();
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

        public Span<int> Indices => CalculateIndices(Signature);

        public Span<int> AntiIndices => CalculateIndices(AntiSignature);

        private static Span<int> CalculateIndices(ulong sig)
        {
            if (sig == 0) return Array.Empty<int>();
            Span<int> indices = new int[(int)Math.Log2(sig)+1];
            var counter = 0;
            for (var i = 0; 1u << i <= sig; i++)
            {
                if(((1u << i) & sig) == 0) continue;
                indices[counter++] = i;
            }

            return indices.Slice(0, counter);
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