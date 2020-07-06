using System;
using System.Collections.Generic;
using ModusOperandi.ECS.Entities;

namespace ModusOperandi.ECS.Components
{
    public interface IComponent
    {
    }

    public interface IComponentManager
    {
        uint NumberOfInstances { get; }
        Entity[] Entities { get; }
    }

    public abstract class ComponentManager : IComponentManager
    {
        public uint NumberOfInstances { get; protected set; } = 512;
        public Entity[] Entities { get; } = new Entity[512];
        public abstract void AddComponent(IComponent component, uint index);
    }

    public class ComponentManager<T> : ComponentManager where T : IComponent
    {
        private readonly Dictionary<uint, uint> _map = new Dictionary<uint, uint>();

        public T[] ManagedComponents { get; } = new T[512];

        public override void AddComponent(IComponent component, uint index)
        {
            ManagedComponents[index] = (T) component;
        }

        private static Instance MakeInstance(int i)
        {
            return new Instance {i = i};
        }

        private Instance LookUp(uint entity)
        {
            return MakeInstance((int) _map.GetValueOrDefault<uint, uint>(entity, 0));
        }

        private void Destroy(uint i)
        {
            var last = NumberOfInstances - 1;
            var entity = Entities[i];
            var lastEntity = Entities[last];
            Entities[i] = Entities[last];
            ManagedComponents[i] = ManagedComponents[last];
            _map[lastEntity] = i;
            _map.Remove(entity);
            NumberOfInstances--;
        }

        private void GarbageCollector(EntityManager entityManager)
        {
            var random = new Random();
            uint aliveInRow = 0;
            while (NumberOfInstances > 0 && aliveInRow < 4)
            {
                var i = (uint) random.Next((int) NumberOfInstances);
                if (entityManager.IsEntityAlive(Entities[i]))
                {
                    aliveInRow++;
                    continue;
                }

                aliveInRow = 0;
                Destroy(i);
            }
        }

        public ref T GetComponent(Entity entity)
        {
            return ref ManagedComponents[entity];
        }

        public ref T GetComponent(uint entity)
        {
            return ref ManagedComponents[entity];
        }

        public struct Instance
        {
            public int i;
        }
    }
}