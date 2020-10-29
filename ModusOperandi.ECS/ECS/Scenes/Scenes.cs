using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.Systems;
using ModusOperandi.Rendering;
using SFML.Graphics;

namespace ModusOperandi.ECS.Scenes
{
    [PublicAPI]
    public abstract class Scene : Drawable
    {
        protected Scene(string name=null)
        {
            Name = name ?? GetType().Name;
        }

        public string Name { get; }
        public EntityManager EntityManager { get; set; } = new();

        private SpriteBatch _spriteBatch = new();
        public virtual void Draw(RenderTarget target, RenderStates states)
        {
            
            _spriteBatch.Begin();
            foreach (var system in GetSystems<DrawSystemAttribute>()) (system as IDrawSystem)?.Draw(_spriteBatch);
            _spriteBatch.End();
            _spriteBatch.Draw(target, states);
        }
        
        /*
        protected Entity PlaceEntity(string type)
        {
            var file = $"{Directories.EntitiesDirectory}{type}.yaml";
            return EntityBuilder.BuildEntity(Yaml.Deserialize<object, object>(file), this);
        }
        */

        public virtual void AddComponentToEntity<T>(T component, Entity entity) where T : unmanaged
        {
            var cm = Ecs.GetComponentManager<T>();
            cm.AddComponent(component, entity);
            Ecs.EntityArchetypes[entity.Index] |= 1u << cm.Index;
        }

        public abstract void Initialize();

        public virtual void Update(float deltaTime)
        {
            foreach (var system in GetSystems<UpdateSystemAttribute>()) (system as IUpdateSystem)?.Execute(deltaTime);
        }

        // TODO: Auto-sort based on dependencies.
        public T StartSystem<T>() where T : ISystem, new()
        {
            var system = new T();
            foreach (var complementarySystem in system.ComplementarySystems)
            {
                AddSystem(complementarySystem);
            }
            AddSystem(system);
            return system;
        }

        public bool AddSystem<T>(T system) where T : ISystem
        {
            return GetSystems((dynamic) GetSystemGroupAttribute<T>()).Add(system);
        }

        public bool StopSystem<T>() where T : ISystem, new()
        {
            return StopSystem(new T());
        }
        
        public bool StopSystem<T>(T dummy) where T : ISystem
        {
            return GetSystems((dynamic) GetSystemGroupAttribute<T>()).Remove(dummy);
        }

        private SystemGroupAttribute GetSystemGroupAttribute<T>() where T : ISystem
        {
            return typeof(T).GetCustomAttribute<SystemGroupAttribute>() ?? new UpdateSystemAttribute();
        }
        
        public void ToggleSystem<T>() where T : ISystem, new()
        {
            if(StopSystem<T>()) return;
            StartSystem<T>();
        }
        
        public static SortedSet<ISystem> GetSystems<T>() where T : SystemGroupAttribute
        {
            return _systems.Get<T>();
        }

        public static SortedSet<ISystem> GetSystems<T>(T _) where T : SystemGroupAttribute
        {
            return GetSystems<T>();
        }
        
        private static SystemsManager _systems = new SystemsManager();
    }

    [PublicAPI]
    public class TypeKeyedCollection<T> where T : new()
    {
        public T Get<TK>()
        {
            var e = PerType<TK>.Element;
            if (e != null) return e;
            Put<TK>(new T());
            return PerType<TK>.Element;
        }
        
        public void Put<TK>(T element)
        {
            PerType<TK>.Element = element;
        }

        private static class PerType<TK>
        {
            public static T Element;
        }
    }

    public class SystemsManager : TypeKeyedCollection<SortedSet<ISystem>>
    {
        public new SortedSet<ISystem> Get<TK>() where TK : SystemGroupAttribute
        {
            return base.Get<TK>();
        }

        public new void Put<TK>(SortedSet<ISystem> element) where TK : SystemGroupAttribute
        {
            base.Put<TK>(element);
        }
    }
}