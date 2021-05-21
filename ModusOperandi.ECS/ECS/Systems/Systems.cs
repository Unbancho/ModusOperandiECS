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
        public bool Parallel { get; set; } = true;
        
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
    public abstract class UpdateEntitySystem<T>: EntitySystem, IUpdateSystem<T> where T: IGameTimeState
    {
        public abstract void PreExecution();
        public virtual void Execute(T gameState)
        {
            foreach (var archetype in Archetypes)
            {
                var entities = Ecs.Query(archetype, Scene);
                if(!Parallel)
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < entities.Length; i++)
                    ActOnEntity(entities[i], gameState);
                else
                {
                    var entitiesP = entities;
                    System.Threading.Tasks.Parallel.For(0, entities.Length, i =>
                    {
                        ActOnEntity(entitiesP[i], gameState);
                    });
                }
            }
        }
        public abstract void ActOnEntity(Entity entity, T gameState);
        public virtual void PostExecution() {}
        public void Run(T gameState)
        {
            PreExecution();
            Execute(gameState);
            PostExecution();
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
        protected int NumberOfComponents => ComponentManager.AssignedComponents;
        public virtual Span<T> ComponentsSpan => ComponentManager.Components.ComponentsSpan;
        public virtual unsafe T* ComponentsPointer => ComponentManager.Components.ComponentsPointer;
        public virtual Components<T> Components => ComponentManager.Components;
    }
    
    [PublicAPI]
    public abstract class UpdateComponentSystem<T1, T2> : ComponentSystem<T1>, IUpdateSystem<T2>  where T1 :
#if UNMANAGED
        unmanaged
#else
        struct
#endif
    where T2 : IGameTimeState
    {
        public virtual void PreExecution()
        {
            
        }

        public unsafe void Execute(T2 gameState)
        {
            var components = Components;
            var ptr = components.ComponentsPointer;
            var length = components.Count;
            if (Parallel)
            {
                System.Threading.Tasks.Parallel.For(0, length, i =>
                {
                    ActOnComponent(ref ptr[i+1], gameState);
                });
            }
            else
            {
                var span = ComponentsSpan;
                for (var i = 0; i < span.Length; i++)
                    ActOnComponent(ref span[i], gameState);   
            }
        }

        public virtual void PostExecution()
        {
            
        }

        public abstract void ActOnComponent(ref T1 component, T2 gameState);
        public void Run(T2 gameState)
        {
            PreExecution();
            Execute(gameState);
            PostExecution();
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
            var span = ComponentsSpan;
            if (Parallel)
            {
                var arr = new T[span.Length];
                span.CopyTo(arr);
                System.Threading.Tasks.Parallel.For(0, arr.Length, i =>
                {
                    DrawComponent(in arr[i], spriteBatch);
                });
            }
            else
            {
                for (var i = 0; i < span.Length; i++)
                    DrawComponent(in span[i], spriteBatch);
            }
        }

        public abstract void DrawComponent(in T component, SpriteBatch spriteBatch);
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