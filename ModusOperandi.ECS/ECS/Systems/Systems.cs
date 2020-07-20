using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.Scenes;

namespace ModusOperandi.ECS.Systems
{
    [PublicAPI]
    public class SystemAttribute : Attribute
    {
        public SystemAttribute(params Type[] systemDependencies)
        {
            SystemDependencies = systemDependencies;
        }

        public Type[] SystemDependencies { get; }
    }

    public interface ISystem
    {
        void Execute(float deltaTime = 0, bool parallel = true, params object[] dependencies);
    }

    [PublicAPI]
    public abstract class System<T> : ISystem where T: unmanaged
    {
        protected const int ArrayStartingSize = 1 << 9;

        protected Entity[] ManagedEntities;

        protected System()
        {
            ManagedEntities = new Entity[ArrayStartingSize];
            SceneManager.GetComponentManager<T>().Entities.Where(c => c.ID != 0).ToArray().CopyTo(ManagedEntities, 0);
        }

        public virtual void Execute(float deltaTime, bool parallel = true, params object[] dependencies)
        {
            Span<Entity> nonNullEntities = ManagedEntities;
            nonNullEntities = nonNullEntities.Slice(0, Array.IndexOf(ManagedEntities, default));
            var entities = nonNullEntities.ToArray();
            if (parallel)
                Parallel.For(0, entities.Length,
                    i => { ActOnComponents(entities[i].ID, (uint) i, deltaTime, dependencies); });
            else
                for (var i = 0; i < nonNullEntities.Length; i++)
                    ActOnComponents(entities[i].ID, (uint) i, deltaTime, dependencies);
        }
        
        protected abstract void ActOnComponents(uint entity, uint index, float deltaTime, params object[] dependencies);

        protected ref TC Get<TC>(uint entity) where TC: unmanaged
        {
            return ref SceneManager.GetComponentManager<TC>().GetComponent(entity);
        }
    }

    public abstract class System<T, T2> : System<T> where T: unmanaged where T2: unmanaged
    {
        protected System()
        {
            var arr = ManagedEntities.Intersect(SceneManager.GetComponentManager<T2>().Entities
                .Where(c => c.ID != 0).ToArray()).ToArray();
            ManagedEntities = new Entity[ArrayStartingSize];
            arr.CopyTo(ManagedEntities, 0);
        }
    }

    public abstract class System<T, T2, T3> : System<T, T2> where T: unmanaged where T2: unmanaged where T3: unmanaged
    {
        protected System()
        {
            var arr = ManagedEntities.Intersect(SceneManager.GetComponentManager<T3>().Entities
                .Where(c => c.ID != 0).ToArray()).ToArray();
            ManagedEntities = new Entity[ArrayStartingSize];
            arr.CopyTo(ManagedEntities, 0);
        }
    }

    public abstract class System<T, T2, T3, T4> : System<T, T2, T3> where T: unmanaged where T4: unmanaged where T2: unmanaged where T3: unmanaged
    {
        protected System()
        {
            var arr = ManagedEntities.Intersect(SceneManager.GetComponentManager<T4>().Entities
                .Where(c => c.ID != 0).ToArray()).ToArray();
            ManagedEntities = new Entity[ArrayStartingSize];
            arr.CopyTo(ManagedEntities, 0);
        }
    }

    [PublicAPI]
    public abstract class SystemGroupAttribute : Attribute
    {
    }

    [PublicAPI]
    public class UpdateSystemAttribute : SystemGroupAttribute
    {
    }

    [PublicAPI]
    public class DrawSystemAttribute : SystemGroupAttribute
    {
    }
}