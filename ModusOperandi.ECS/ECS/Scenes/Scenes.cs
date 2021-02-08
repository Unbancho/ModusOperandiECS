using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using ModusOperandi.ECS.Components;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.EntityBuilding;
using ModusOperandi.ECS.Systems;
using ModusOperandi.ECS.Systems.SystemAttributes;
using ModusOperandi.ECS.Systems.SystemInterfaces;
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
        public Entity[] Entities => EntityManager.CreatedEntities;

        private SpriteBatch _spriteBatch = new();
        public virtual void Draw(RenderTarget target, RenderStates states)
        {
            _spriteBatch.Begin();
            foreach (var system in GetSystems<DrawSystemAttribute>()) (system as IDrawSystem)?.Draw(_spriteBatch);
            _spriteBatch.End();
            _spriteBatch.Draw(target, states);
        }

        public Entity PlaceEntity(string type)
        {
            return EntityBuilder.BuildEntity(EntityBuilder.LoadEntityComponents(type), this);
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
            system.Scene = this;
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
        
        public static HashSet<ISystem> GetSystems<T>() where T : SystemGroupAttribute
        {
            return _systems.Get<T>();
        }

        public static HashSet<ISystem> GetSystems<T>(T _) where T : SystemGroupAttribute
        {
            return GetSystems<T>();
        }
        
        private static SystemsManager _systems = new();
        
        private static class PerEventType<T> where T: IEntityEvent
        {
            public static readonly List<IListenSystem<T>> Listeners = new();
        }
        
        public static List<IListenSystem<T>> GetListeners<T>() where T : IEntityEvent => PerEventType<T>.Listeners;
        public static void RegisterListener<T>(IListenSystem<T> listener) where T : IEntityEvent
            => PerEventType<T>.Listeners.Add(listener);
    }

    [PublicAPI]
    public class TypeKeyedCollection<T> where T : new()
    {
        public T Get<TK>()
        {
            var e = PerType<TK>.Element;
            if (e != null) return e;
            Put<TK>(new());
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

    public class SystemsManager : TypeKeyedCollection<HashSet<ISystem>>
    {
        public new HashSet<ISystem> Get<TK>() where TK : SystemGroupAttribute
        {
            return base.Get<TK>();
        }

        public new void Put<TK>(HashSet<ISystem> element) where TK : SystemGroupAttribute
        {
            base.Put<TK>(element);
        }
    }
}