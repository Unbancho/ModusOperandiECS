using System;
using JetBrains.Annotations;
using ModusOperandi.ECS.Components;

namespace ModusOperandi.ECS.Entities
{
    [PublicAPI]
    [Component]
    public readonly struct Entity : IEquatable<Entity>
    {
        public static implicit operator uint(Entity entity) => entity.ID;
        public static implicit operator Entity(uint id) => new(id);

        private const byte EntityIndexBits = 20;
        private const byte EntityGenerationBits = 32-EntityIndexBits;
        private const uint EntityIndexMask = (1 << EntityIndexBits) - 1;
        private const uint EntityGenerationMask = (1 << EntityGenerationBits) - 1;

        // ReSharper disable once InconsistentNaming
        public readonly uint ID;
        
        //TODO: Figure out where this should be used
        public uint Index => ID & EntityIndexMask;
        public uint Generation => (ID >> EntityIndexBits) & EntityGenerationMask;

        private Entity(uint id)
        {
            ID = id;
        }

        public bool Equals(Entity other)
        {
            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            return obj is Entity entity && Equals(entity);
        }

        public static bool operator ==(Entity left, Entity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode() + 17 * 23;
        }
    }

    [PublicAPI]
    public static class EntityExtensions
    {
        public static bool IsNullEntity(this Entity entity) => entity.ID == 0u;

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Get<T>(this Entity entity) where T :
#if UNMANAGED
            unmanaged
#else 
            struct
#endif
            => ref Ecs.GetComponentManager<T>().GetComponent(entity);
        
        
        public static ref T Get<T>(this Entity entity, T _) where T :
#if UNMANAGED
            unmanaged
#else 
            struct
#endif
            => ref Ecs.GetComponentManager<T>().GetComponent(entity);
    }
}