#define UNMANAGED

using JetBrains.Annotations;
using ModusOperandi.ECS.Components;

namespace ModusOperandi.ECS.Archetypes
{

    public interface IArchetype
    {
        public ulong Signature { get; }
        public ulong AntiSignature { get;}
    }
    
    public readonly struct Archetype : IArchetype
    {
        public ulong Signature { get; }
        public ulong AntiSignature { get; }

        public Archetype(ulong signature, ulong antiSignature)
        {
            Signature = signature;
            AntiSignature = antiSignature;
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
        
        public static implicit operator Archetype(Archetype<T> a) => new Archetype(a.Signature, a.AntiSignature);
    }
}