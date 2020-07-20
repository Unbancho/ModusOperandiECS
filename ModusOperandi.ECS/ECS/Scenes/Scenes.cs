using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.EntityBuilding;
using ModusOperandi.ECS.Systems;
using ModusOperandi.Utils.YAML;
using SFML.Graphics;

namespace ModusOperandi.ECS.Scenes
{
    [PublicAPI]
    public abstract class Scene : Drawable
    {
        protected Scene()
        {
            Name = GetType().Name;
            EntityManager = new EntityManager();
        }

        public string Name { get; }
        public List<Entity> Entities { get; } = new List<Entity>();
        public EntityManager EntityManager { get; set; }


        // TODO: Make good lol.
        public virtual void Draw(RenderTarget target, RenderStates states)
        {
            foreach (var system in GetSystems<DrawSystemAttribute>()) system.Execute(0, false, target, states);
        }

        protected Entity PlaceEntity(string type)
        {
            var file = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/Entities/{type}.yaml";
            return EntityBuilder.BuildEntity(Yaml.Deserialize<object, object>(file), this);
        }

        public virtual void AddComponentToEntity<T>(T component, Entity entity, params object[] componentParams) where T: unmanaged
        {
            var cm = SceneManager.GetComponentManager<T>();
            cm.Entities[entity.Index] = entity;
            cm.AddComponent(component, entity.Index);
        }

        public virtual void Initialize()
        {
            // TODO: Do something.
        }

        public virtual void Update(float deltaTime)
        {
            foreach (var system in GetSystems<UpdateSystemAttribute>()) system.Execute(deltaTime);
        }

        // TODO: Auto-sort based on dependencies.
        public T StartSystem<T>() where T : ISystem, new()
        {
            var system = new T();
            var groupType = system.GetType().GetCustomAttribute(typeof(SystemGroupAttribute))?.GetType();
            if (groupType == null)
                groupType = typeof(UpdateSystemAttribute);
            GetSystems((dynamic) Activator.CreateInstance(groupType)).Add(system);
            _allSystems.Add(system);
            return system;
        }

        public void StopSystem<T>() where T : ISystem
        {
            var groupType = typeof(T).GetCustomAttribute(typeof(SystemGroupAttribute))?.GetType();
            var systemsList = GetSystems((dynamic) Activator.CreateInstance(groupType));
            foreach (var system in systemsList)
            {
                if (system.GetType() != typeof(T)) continue;
                systemsList.Remove(system);
                return;
            }
        }
        
        public void StopSystem<T>(T _) where T : ISystem
        {
            StopSystem<T>();
        }

        private List<ISystem> _allSystems = new List<ISystem>();
        public List<ISystem> GetAllSystems()
        {
            return _allSystems;
        }

        public static List<ISystem> GetSystems<T>()
        {
            return PerType<T>.Systems;
        }

        public static List<ISystem> GetSystems<T>(T _)
        {
            return GetSystems<T>();
        }

        private static class PerType<T>
        {
            public static readonly List<ISystem> Systems = new List<ISystem>();
        }
    }
}