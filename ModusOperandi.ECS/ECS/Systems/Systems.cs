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
        protected System()
        {
            ManagedEntities = SceneManager.GetComponentManager<T>().Entities
                .Where(c => c.ID != 0).ToArray();
        }

        public Entity[] ManagedEntities { get; protected set; }

        public virtual void Execute(float deltaTime, params object[] dependencies)
        {
            for (uint i = 0; i < ManagedEntities.Length; i++)
            {
                var e = ManagedEntities[i];
                if (e.IsNullEntity())
                    break;
                ActOnComponents(ManagedEntities[i].ID, i, deltaTime, dependencies);
            }
        }

        protected abstract void ActOnComponents(uint entity, uint index, float deltaTime, params object[] dependencies);
    }

    public abstract class System<T, T2> : System<T> where T : IComponent
        where T2 : IComponent
    {
        protected System()
        {
            ManagedEntities = ManagedEntities.Intersect(SceneManager.GetComponentManager<T2>().Entities
                .Where(c => c.ID != 0).ToArray()).ToArray();
        }
    }

    public abstract class System<T, T2, T3> : System<T, T2> where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        protected System()
        {
            ManagedEntities = ManagedEntities.Intersect(SceneManager.GetComponentManager<T3>().Entities
                .Where(c => c.ID != 0).ToArray()).ToArray();
        }
    }

    public abstract class System<T, T2, T3, T4> : System<T, T2, T3> where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        protected System()
        {
            ManagedEntities = ManagedEntities.Intersect(SceneManager.GetComponentManager<T4>().Entities
                .Where(c => c.ID != 0).ToArray()).ToArray();
        }
    }
}