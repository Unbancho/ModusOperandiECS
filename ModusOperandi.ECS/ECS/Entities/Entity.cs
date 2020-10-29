using JetBrains.Annotations;
using ModusOperandi.ECS.Components;

namespace ModusOperandi.ECS.Entities
{
    [PublicAPI]
    [Component]
    public readonly struct Entity
    {
        public static implicit operator uint(Entity entity) => entity.ID;
        public static implicit operator Entity(uint id) => new Entity(id);

        private const int EntityIndexBits = 16;
        private const int EntityGenerationBits = 32-EntityIndexBits;
        private const uint EntityIndexMask = (1 << EntityIndexBits) - 1;
        private const uint EntityGenerationMask = (1 << EntityGenerationBits) - 1;

        // ReSharper disable once InconsistentNaming
        public readonly uint ID;
        public uint Index => ID & EntityGenerationMask;
        public uint Generation => (ID >> EntityIndexBits) & EntityGenerationMask;

        private Entity(uint id)
        {
            ID = id;
        }
    }

    [PublicAPI]
    public static class EntityExtensions
    {
        public static bool IsNullEntity(this Entity entity) => entity.ID == 0;

        public static ref T Get<T>(this Entity entity) where T : unmanaged 
            => ref Ecs.GetComponentManager<T>().GetComponent(entity);
    }
}