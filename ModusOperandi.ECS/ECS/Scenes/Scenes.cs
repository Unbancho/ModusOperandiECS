using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.EntityBuilding;
using ModusOperandi.ECS.Systems;
using ModusOperandi.Rendering;
using ModusOperandi.Utils.YAML;
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
        
        protected Entity PlaceEntity(string type)
        {
            var file = $"{Directories.EntitiesDirectory}{type}.yaml";
            return EntityBuilder.BuildEntity(Yaml.Deserialize<object, object>(file), this);
        }

        public virtual void AddComponentToEntity<T>(T component, Entity entity) where T : unmanaged
        {
            var cm = Ecs.GetComponentManager<T>();
            cm.AddComponent(component, entity);
            Ecs.ComponentArrays[cm.Index, entity.Index] = entity;
        }

        public abstract void Initialize();

        public virtual void Update(float deltaTime)
        {
            foreach (var system in GetSystems<UpdateSystemAttribute>()) (system as IUpdateSystem)?.Execute(deltaTime);
        }

        // TODO: Auto-sort based on dependencies.
        // TODO: Rework this.
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

        public void AddSystem<T>(T system) where T : ISystem
        {
            var attribute = typeof(T).GetCustomAttribute<SystemGroupAttribute>() ?? new UpdateSystemAttribute();
            GetSystems((dynamic)attribute).Add(system);
        }

        public bool StopSystem<T>() where T : ISystem
        {
            var attribute = typeof(T).GetCustomAttribute<SystemGroupAttribute>() ?? new UpdateSystemAttribute();
            var systemsList = GetSystems((dynamic) attribute);
            foreach (var system in systemsList)
            {
                if (system.GetType() != typeof(T)) continue;
                systemsList.Remove(system);
                return true;
            }

            return false;
        }

        public void ToggleSystem<T>() where T : ISystem, new()
        {
            if(StopSystem<T>()) return;
            StartSystem<T>();
        }

        public void StopSystem<T>(T _) where T : ISystem
        {
            StopSystem<T>();
        }

        public static List<ISystem> GetSystems<T>()
        {
            return PerType<T>.Systems;
        }

        public static List<ISystem> GetSystems<T>(T _)
        {
            return GetSystems<T>();
        }

        // ReSharper disable once UnusedTypeParameter
        private static class PerType<T>
        {
            // ReSharper disable once StaticMemberInGenericType
            public static readonly List<ISystem> Systems = new List<ISystem>();
        }
    }
}