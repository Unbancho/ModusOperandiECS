using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace ModusOperandi.ECS.Entities
{
    [PublicAPI]
    public class EntityManager
    {
        private static ulong _minimumFreeIndices = Ecs.MaxEntities;
        private readonly List<uint> _freeIndices = new List<uint>();
        private readonly List<byte> _generation = new List<byte>();

        public Entity CreateEntity()
        {
            uint index;
            if ((ulong)_freeIndices.Count > _minimumFreeIndices)
            {
                index = _freeIndices.First();
                _freeIndices.Remove(index);
            }
            else
            {
                _generation.Add(0);
                index = (uint) _generation.Count; // Removed -1 to have 0 be null
            }

            return index;
        }

        public bool IsEntityAlive(Entity entity)
        {
            return _generation[(int) entity.Index] == entity.Generation;
        }

        public void DestroyEntity(Entity entity)
        {
            var index = entity.Index;
            ++_generation[(int) index];
            _freeIndices.Add(index);
        }

        public ulong NumberOfAliveEntities => (ulong)_generation.Count + 1;
    }
}