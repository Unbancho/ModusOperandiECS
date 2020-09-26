using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using ModusOperandi.ECS.Archetypes;
using ModusOperandi.ECS.Components;
using ModusOperandi.ECS.Entities;
using ModusOperandi.Rendering;

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
    
    [PublicAPI]
    public interface ISystem
    {
        List<ISystem> ComplementarySystems { get; }
        bool Parallel { get; set; }
    }

    [PublicAPI]
    public interface IEntitySystem : ISystem
    {
        Archetype Archetype { get; }
    }

    [PublicAPI]
    public interface IComponentSystem<T> : ISystem where T : unmanaged
    {
        Span<T> Components { get; }
    }

    [PublicAPI]
    public interface IUpdateSystem : ISystem
    {
        void Execute(float deltaTime);
    }

    [PublicAPI]
    public interface IDrawSystem : ISystem
    {
        void Draw(SpriteBatch spriteBatch);
    }

    [PublicAPI]
    public abstract class UpdateEntitySystem : IEntitySystem, IUpdateSystem
    {
        public Archetype Archetype { get; }
        public List<ISystem> ComplementarySystems { get; } = new ();
        public bool Parallel { get; set; } = true;
        
        
        protected abstract void ActOnEntity(uint entity, float deltaTime);

        protected UpdateEntitySystem(Archetype archetype)
        {
            Archetype = archetype;
            // ReSharper disable once HeapView.ObjectAllocation
            Ecs.ArchetypeEntityDictionary[Archetype.Signature] = Ecs.Query(Archetype).ToArray();
        }
        
        public virtual void Execute(float deltaTime)
        {
            var entities = Ecs.ArchetypeEntityDictionary[Archetype.Signature];
            if (Parallel)
                ActOnEntitiesParallel(deltaTime, entities);
            else
                ActOnEntities(deltaTime, entities);
        }

        private void ActOnEntities(float deltaTime, Entity[] entities)
        {
            for (var i = 0; i < entities.Length; i++)
                ActOnEntity(entities[i].ID, deltaTime);
        }
        
        // ReSharper disable once HeapView.ClosureAllocation
        private void ActOnEntitiesParallel(float deltaTime, Entity[] entities)
        {
            var degreeOfParallelism = Environment.ProcessorCount;
            // ReSharper disable once HeapView.ObjectAllocation.Evident
            var tasks = new Task[degreeOfParallelism];
            for (var taskNumber = 0; taskNumber < degreeOfParallelism; taskNumber++)
            {
                // ReSharper disable once HeapView.ClosureAllocation
                var taskNumberCopy = taskNumber;
                tasks[taskNumber] = Task.Factory.StartNew(
                    // ReSharper disable once HeapView.DelegateAllocation
                    () =>
                    {
                        var max = entities.Length * (taskNumberCopy + 1) / degreeOfParallelism;
                        for (var i = entities.Length * taskNumberCopy / degreeOfParallelism; i < max; i++)
                            ActOnEntity(entities[i].ID, deltaTime);
                    });
            }

            Task.WaitAll(tasks);
        }
        
        protected ref T Get<T>(uint entity) where T : unmanaged
        {
            return ref Ecs.GetComponentManager<T>().GetComponent(entity);
        }
    }

    [PublicAPI]
    public abstract class ComponentSystem<T> : IComponentSystem<T> where T : unmanaged
    {
        protected ComponentManager<T> ComponentManager => Ecs.GetComponentManager<T>();
        protected uint NumberOfComponents => ComponentManager.AssignedComponents;
        public Span<T> Components => ((Span<T>)ComponentManager.ManagedComponents).Slice(1, (int)NumberOfComponents);
        public List<ISystem> ComplementarySystems { get; } = new ();
        public bool Parallel { get; set; }
    }
    
    [PublicAPI]
    public abstract class UpdateComponentSystem<T> : ComponentSystem<T>, IUpdateSystem where T : unmanaged
    {
        public virtual void Execute(float deltaTime)
        {
            var components = Components;
            for (var i = 0; i < components.Length; i++)
                ActOnComponent(ref components[i], deltaTime);
        }

        public abstract void ActOnComponent(ref T component, float deltaTime);
    }

    [PublicAPI]
    public abstract class DrawComponentSystem<T> : ComponentSystem<T>, IDrawSystem where T : unmanaged
    {
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            var components = Components;
            for (var i = 0; i < components.Length; i++)
                DrawComponent(ref components[i], spriteBatch);
        }

        public abstract void DrawComponent(ref T component, SpriteBatch spriteBatch);
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