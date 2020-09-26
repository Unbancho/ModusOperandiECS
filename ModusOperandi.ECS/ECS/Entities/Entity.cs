using JetBrains.Annotations;

namespace ModusOperandi.ECS.Entities
{
    [PublicAPI]
    public struct Entity
    {
        public static implicit operator uint(Entity entity) => entity.ID;

        private const int EntityIndexBits = 16;
        private const uint EntityIndexMask = (1 << EntityIndexBits) - 1;
        private const int EntityGenerationBits = 16;
        private const uint EntityGenerationMask = (1 << EntityGenerationBits) - 1;

        // ReSharper disable once InconsistentNaming
        public uint ID;
        public uint Index => ID & EntityGenerationMask;
        public uint Generation => (ID >> EntityIndexBits) & EntityGenerationMask;
    }

    [PublicAPI]
    public static class EntityExtensions
    {
        public static bool IsNullEntity(this Entity entity) => entity.ID == 0;
    }
}