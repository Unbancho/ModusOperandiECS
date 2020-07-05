using System.Collections.Generic;
using System.Linq;

namespace ModusOperandi.ECS.Entities
{
    public class EntityManager
    {
        private const uint MinimumFreeIndices = 1024;
        private readonly List<uint> _freeIndices = new List<uint>();
        private readonly List<byte> _generation = new List<byte>();

        public Entity CreateEntity()
        {
            uint index;
            if (_freeIndices.Count > MinimumFreeIndices)
            {
                index = _freeIndices.First();
                _freeIndices.Remove(index);
            }
            else
            {
                _generation.Add(0);
                index = (uint) _generation.Count; // Removed -1 to have 0 be null
            }

            return new Entity
            {
                ID = index
            };
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
    }
}