using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using ModusOperandi.ECS.Entities;

namespace ModusOperandi.ECS.Components
{
    public class Component : Attribute
    {
    }

    [PublicAPI]
    public interface IComponentManager
    {
        uint NumberOfInstances { get; }
        Entity[] Entities { get; }
    }

    [PublicAPI]
    public abstract class ComponentManager : IComponentManager
    {
        public uint NumberOfInstances { get; protected set; } = 512;
        public Entity[] Entities { get; } = new Entity[512];
    }

    internal static class SignatureCounter
    {
        public static int Counter = 0;
    }

    [PublicAPI]
    public class ComponentManager<T> : ComponentManager where T : unmanaged
    {
        public static ulong Signature { get; private set; }
        
        private readonly Dictionary<uint, uint> _map = new Dictionary<uint, uint>();

        public ComponentManager()
        {
            Signature = (ulong) 1 << SignatureCounter.Counter++;
            ManagedComponents = new T[Entities.Length];
        }

        public T[] ManagedComponents { get; }

        public uint AssignedComponents { get; set; }

        public void AddComponent(T component, uint entity)
        {
            ManagedComponents[entity] = component;
            AssignedComponents++;
            _map[AssignedComponents - 1] = entity;
        }

        public uint LookUp(uint entity)
        {
            return _map.GetValueOrDefault<uint, uint>(entity, 0);
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
    }
}