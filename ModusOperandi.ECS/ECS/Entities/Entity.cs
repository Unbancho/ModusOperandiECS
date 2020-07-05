namespace ModusOperandi.ECS.Entities
{
    public struct Entity
    {
        public static implicit operator uint(Entity entity)
        {
            return entity.ID;
        }

        private const int EntityIndexBits = 22;
        private const uint EntityIndexMask = (1 << EntityIndexBits) - 1;
        private const int EntityGenerationBits = 22;
        private const uint EntityGenerationMask = (1 << EntityGenerationBits) - 1;

        public uint ID;
        public uint Index => ID & EntityGenerationMask;
        public uint Generation => (ID >> EntityIndexBits) & EntityGenerationMask;
    }

    public static class EntityExtensions
    {
        public static bool IsNullEntity(this Entity entity)
        {
            return entity.ID == 0;
        }
    }
}