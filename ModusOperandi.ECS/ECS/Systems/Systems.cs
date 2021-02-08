using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using ModusOperandi.ECS.Archetypes;
using ModusOperandi.ECS.Components;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.Scenes;
using ModusOperandi.ECS.Systems.SystemInterfaces;
using ModusOperandi.Rendering;

namespace ModusOperandi.ECS.Systems
{
    public interface IEntityEvent
    {
        public Entity Sender { get; }
    }

    public abstract class UniqueSystem : ISystem
    {
        public Scene Scene { get; set; }
        public List<ISystem> ComplementarySystems { get; } = new();
        public bool Parallel { get; set; }

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return GetHashCode() == obj?.GetHashCode();
        }
    }

    [PublicAPI]
    public abstract class EntitySystem<T> : UniqueSystem, IEntitySystem<T> where T : IArchetype, new()
    {
        public Archetype[] Archetypes { get; protected set; }
    }

    [PublicAPI]
    public abstract class UpdateEntitySystem<T> : EntitySystem<T>, IUpdateSystem where T : IArchetype, new()
    {
        protected UpdateEntitySystem()
        {
            var arch = new T();
            Archetypes = new[] {new Archetype(arch.Signature, arch.AntiSignature, arch.Indices, arch.AntiIndices)};
        }
        
        public abstract void ActOnEntity(Entity entity, float deltaTime);

        public virtual void Execute(float deltaTime)
        {
            foreach (var archetype in Archetypes)
            {
                var entities = Ecs.Query(archetype, Scene);
                if (Parallel)
                    ActOnEntitiesParallel(deltaTime, entities);
                else
                    ActOnEntities(deltaTime, entities);
            }
        }

        public void ActOnEntities(float deltaTime, Span<Entity> entities)
        {
            for (var i = 0; i < entities.Length; i++)
                ActOnEntity(entities[i], deltaTime);
        }
        
        // TODO: Find out why it's so much slower, maybe make it not so.
        private void ActOnEntitiesParallel(float deltaTime, Span<Entity> entitiesSpan)
        {
            var entities = entitiesSpan.ToArray();
            
            var degreeOfParallelism = Environment.ProcessorCount;
            var tasks = new Task[degreeOfParallelism];
            for (var taskNumber = 0; taskNumber < degreeOfParallelism; taskNumber++)
            {
                var taskNumberCopy = taskNumber;
                tasks[taskNumber] = Task.Factory.StartNew(
                    () =>
                    {
                        var max = entities.Length * (taskNumberCopy + 1) / degreeOfParallelism;
                        for (var i = entities.Length * taskNumberCopy / degreeOfParallelism; i < max; i++)
                            ActOnEntity(entities[i].ID, deltaTime);
                    });
            }

            Task.WaitAll(tasks);
        }
    }

    [PublicAPI]
    public abstract class ComponentSystem<T> : UniqueSystem, IComponentSystem<T> where T :
#if UNMANAGED
        unmanaged
#else
        struct
#endif
    {
        protected ComponentManager<T> ComponentManager => Ecs.GetComponentManager<T>();
        protected uint NumberOfComponents => ComponentManager.AssignedComponents;
        public Span<T> Components => ((Span<T>)ComponentManager.ManagedComponents).Slice(1, (int)NumberOfComponents);
    }
    
    [PublicAPI]
    public abstract class UpdateComponentSystem<T> : ComponentSystem<T>, IUpdateSystem  where T :
#if UNMANAGED
        unmanaged
#else
        struct
#endif
    {
        public virtual void Execute(float deltaTime)
        {
            var components = Components;
            for (var i = 0; i < components.Length; i++)
                ActOnComponent(ref components[i], deltaTime);
        }

        public virtual void ActOnComponent(ref T component, float deltaTime)
        {
            throw new NotImplementedException();
        }
    }

    [PublicAPI]
    public abstract class DrawComponentSystem<T> : ComponentSystem<T>, IDrawSystem  where T :
#if UNMANAGED
        unmanaged
#else
        struct
#endif
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
    public abstract class EventListenerSystem<T> : UniqueSystem, IEntitySystem, IListenSystem<T> where T : IEntityEvent
    {
        public ConcurrentStack<T> Events { get; } = new();

        protected EventListenerSystem()
        {
            Scene.RegisterListener(this);
        }

        public Archetype[] Archetypes { get; protected set; }

        public virtual bool ValidEvent(ref T e)
        {
            foreach (var archetype in Archetypes)
            {
                if((Ecs.GetEntityArchetype(e.Sender) & archetype.Signature) == archetype.Signature) return true;
            }

            return false;
        }
    }

    [PublicAPI]
    public abstract class EventEmitterSystem<T> : IEntitySystem, IEmitSystem<T> where T : IEntityEvent
    {
        public virtual void Emit(T e)
        {
            foreach (var l in Scene.GetListeners<T>())
            {
                var els = (l as EventListenerSystem<T>)?.ValidEvent(ref e);
                if (els != null)
                {
                    if(els.Value)
                        l.Events.Push(e);
                    continue;
                }
                l.Events.Push(e);
            }
        }

        public Scene Scene { get; set; }
        public List<ISystem> ComplementarySystems { get; } = new();
        public bool Parallel { get; set; }
        public Archetype[] Archetypes { get; protected set; }
    }
}