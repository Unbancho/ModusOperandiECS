using System;
using System.Linq;
using ModusOperandi.ECS.Components;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.Scenes;

namespace ModusOperandi.ECS.Systems
{
    public interface ISystem
    {
        void Execute(float deltaTime = 0, params object[] dependencies);
    }

    public abstract class System<T> : ISystem where T : IComponent
    {
        protected const int ArrayStartingSize = 1 << 9;

        protected System()
        {
            ManagedEntities = new Entity[ArrayStartingSize];
            SceneManager.GetComponentManager<T>().Entities
                .Where(c => c.ID != 0).ToArray().CopyTo(ManagedEntities, 0);
        }

        protected Entity[] ManagedEntities;

        public virtual void Execute(float deltaTime, params object[] dependencies)
        {
            Span<Entity> nonNullEntities = ManagedEntities;
            nonNullEntities = nonNullEntities.Slice(0, Array.IndexOf(ManagedEntities, default(Entity)));
            for (int i = 0; i < nonNullEntities.Length; i++)
            {
                ActOnComponents(nonNullEntities[i].ID, (uint)i, deltaTime, dependencies);
            }
        }

        protected abstract void ActOnComponents(uint entity, uint index, float deltaTime, params object[] dependencies);

        protected ref TC Get<TC>(uint entity) where TC : IComponent
        {
            return ref SceneManager.GetComponentManager<TC>().GetComponent(entity);
        }
    }

    public abstract class System<T, T2> : System<T> where T : IComponent
        where T2 : IComponent
    {
        protected System()
        {
            var arr = ManagedEntities.Intersect(SceneManager.GetComponentManager<T2>().Entities
                .Where(c => c.ID != 0).ToArray()).ToArray();
            ManagedEntities = new Entity[ArrayStartingSize];
            arr.CopyTo(ManagedEntities, 0);
        }
    }

    public abstract class System<T, T2, T3> : System<T, T2> where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        protected System()
        {
            var arr = ManagedEntities.Intersect(SceneManager.GetComponentManager<T3>().Entities
                .Where(c => c.ID != 0).ToArray()).ToArray();
            ManagedEntities = new Entity[ArrayStartingSize];
            arr.CopyTo(ManagedEntities, 0);
        }
    }

    public abstract class System<T, T2, T3, T4> : System<T, T2, T3> where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        protected System()
        {
            var arr = ManagedEntities.Intersect(SceneManager.GetComponentManager<T4>().Entities
                .Where(c => c.ID != 0).ToArray()).ToArray();
            ManagedEntities = new Entity[ArrayStartingSize];
            arr.CopyTo(ManagedEntities, 0);
        }
    }
}