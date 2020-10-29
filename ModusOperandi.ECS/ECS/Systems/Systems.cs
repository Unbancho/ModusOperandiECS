using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using ModusOperandi.ECS.Archetypes;
using ModusOperandi.ECS.Components;
using ModusOperandi.ECS.Entities;
using ModusOperandi.Rendering;

namespace ModusOperandi.ECS.Systems
{
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

    //TODO: Refactor, especially IComparable
    [PublicAPI]
    public abstract class Singleton : IComparable
    {
        public int CompareTo(object? obj)
        {
            if (obj == null) return 1;
            var attribute = GetType().GetCustomAttribute<SystemGroupAttribute>();
            if (attribute == null) return 1;
            var t= ((dynamic)obj).GetType();
            foreach (var d in attribute.SystemDependencies)
            {
                if (t == d) return 1;
            }
            attribute = ((Type) t).GetCustomAttribute<SystemGroupAttribute>();
            if (attribute == null) return 1;
            foreach (var d in attribute.SystemDependencies)
            {
                if (GetType() == d) return -1;
            }
            return Equals(obj) ? 0 : 1;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Singleton);
        }

        public bool Equals(Singleton s)
        {
            if (ReferenceEquals(s, null)) return false;
            if (ReferenceEquals(this, s)) return true;
            return GetType() == s.GetType();
        }

        public static bool operator ==(Singleton left, Singleton right) => left?.Equals(right) ?? ReferenceEquals(right, null);
        public static bool operator !=(Singleton left, Singleton right) => !(left == right);
    }

    [PublicAPI]
    public abstract class UpdateEntitySystem : Singleton, IEntitySystem, IUpdateSystem
    {
        public Archetype Archetype { get; }
        public List<ISystem> ComplementarySystems { get; } = new ();
        public bool Parallel { get; set; } = true;
        
        
        protected abstract void ActOnEntity(Entity entity, float deltaTime);

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
                ActOnEntity(entities[i], deltaTime);
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
    }

    [PublicAPI]
    public abstract class ComponentSystem<T> : Singleton, IComponentSystem<T> where T : unmanaged
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
        protected SystemGroupAttribute(params Type[] systemDependencies)
        {
            SystemDependencies = systemDependencies;
        }

        public Type[] SystemDependencies { get; }
    }

    [PublicAPI]
    public class UpdateSystemAttribute : SystemGroupAttribute
    {
        public UpdateSystemAttribute(params Type[] systemDependencies) : base(systemDependencies)
        {
            
        }
    }

    [PublicAPI]
    public class DrawSystemAttribute : SystemGroupAttribute
    {
        public DrawSystemAttribute(params Type[] systemDependencies) : base(systemDependencies)
        {
            
        }
    }
}