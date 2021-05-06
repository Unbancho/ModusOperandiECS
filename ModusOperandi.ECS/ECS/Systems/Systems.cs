using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    public abstract class EntitySystem : UniqueSystem, IEntitySystem
    {
        public Archetype[] Archetypes { get; protected set; }
        
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        public class IncludeAttribute : Attribute
        {
            public ulong Signature { get; private set; }
            public IncludeAttribute(Type componentType)
            {
                LoadSignature((dynamic)Activator.CreateInstance(componentType));
            }

            private void LoadSignature<T>(T _) where T:
#if UNMANAGED
                unmanaged
#else
            struct
#endif
            {
                Signature = Ecs.GetSignature<T>();
            }
        }
        
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        public class ExcludeAttribute : Attribute
        {
            public ulong Signature { get; private set; }
            public ExcludeAttribute(Type componentType)
            {
                LoadSignature((dynamic)Activator.CreateInstance(componentType));
            }

            private void LoadSignature<T>(T _) where T:
#if UNMANAGED
                unmanaged
#else
            struct
#endif
            {
                Signature = Ecs.GetSignature<T>();
            }
        }

        protected EntitySystem()
        {
            var sig = 0ul;
            var includes = GetType().GetCustomAttributes(typeof(IncludeAttribute), false)
                .Cast<IncludeAttribute>();
            foreach (var include in includes)
            {
                sig |= include.Signature;
            }
            var excludes = GetType().GetCustomAttributes(typeof(ExcludeAttribute), false)
                .Cast<ExcludeAttribute>();
            var antiSig = 0ul;
            foreach (var exclude in excludes)
            {
                antiSig |= exclude.Signature;
            }

            Archetypes = new[] { new Archetype(sig, antiSig)};
        }
    }

    [PublicAPI]
    public abstract class UpdateEntitySystem: EntitySystem, IUpdateSystem
    {
        public virtual void PreExecution() {}

        public virtual void Execute(float deltaTime)
        {
            foreach (var archetype in Archetypes)
            {
                var entities = Ecs.Query(archetype, Scene);
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < entities.Length; i++)
                    ActOnEntity(entities[i], deltaTime);
            }
        }
        
        public abstract void ActOnEntity(Entity entity, float deltaTime);

        public virtual void PostExecution() {}
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
        protected int NumberOfComponents => ComponentManager.AssignedComponents;
        public virtual Span<T> Components => ComponentManager.Components.ComponentArray;
    }
    
    [PublicAPI]
    public abstract class UpdateComponentSystem<T> : ComponentSystem<T>, IUpdateSystem  where T :
#if UNMANAGED
        unmanaged
#else
        struct
#endif
    {
        public virtual void PreExecution()
        {
            
        }

        public virtual void Execute(float deltaTime)
        {
            var components = Components;
            for (var i = 0; i < components.Length; i++)
                ActOnComponent(ref components[i], deltaTime);
        }

        public virtual void PostExecution()
        {
            
        }

        public abstract void ActOnComponent(ref T component, float deltaTime);
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
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < components.Length; i++)
                DrawComponent(components[i], spriteBatch);
        }

        public abstract void DrawComponent(T component, SpriteBatch spriteBatch);
    }

    [PublicAPI]
    public abstract class EventListenerSystem<T> : EntitySystem, IListenSystem<T> where T : IEntityEvent
    {
        public ConcurrentStack<T> Events { get; } = new();

        protected EventListenerSystem()
        {
            Scene.RegisterListener(this);
        }
        
        public virtual bool ValidEvent(in T e)
        {
            for (var index = 0; index < Archetypes.Length; index++)
            {
                var archetype = Archetypes[index];
                if ((Ecs.GetEntityArchetype(e.Sender) & archetype.Signature) == archetype.Signature) return true;
            }

            return false;
        }
    }

    [PublicAPI]
    public abstract class EventEmitterSystem<T> : IEntitySystem, IEmitSystem<T> where T : IEntityEvent
    {
        public virtual void Emit(T e)
        {
            var ls = Scene.GetListeners<T>();
            for (var index = 0; index < ls.Count; index++)
            {
                var l = ls[index];
                var els = (l as EventListenerSystem<T>)?.ValidEvent(in e);
                if (els != null)
                {
                    if (els.Value)
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