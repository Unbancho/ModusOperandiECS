using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using ModusOperandi.ECS.Components;
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
        protected Scene()
        {
            Name = GetType().Name;
            EntityManager = new EntityManager();
        }

        public string Name { get; }
        public EntityManager EntityManager { get; set; }


        // TODO: Make good lol.
        public struct Context
        {
            public RenderTarget Target;
            public RenderStates States;
        }

        public virtual void Draw(RenderTarget target, RenderStates states)
        {
            var spriteBatch = new SpriteBatch();
            spriteBatch.Begin();
            foreach (var system in GetSystems<DrawSystemAttribute>()) ((IDrawSystem)system).Draw(spriteBatch);
            spriteBatch.End();
            spriteBatch.Draw(target, states);
        }
        
        protected Entity PlaceEntity(string type)
        {
            var file = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/Entities/{type}.yaml";
            return EntityBuilder.BuildEntity(Yaml.Deserialize<object, object>(file), this);
        }

        public virtual void AddComponentToEntity<T>(T component, Entity entity) where T : unmanaged
        {
            var cm = SceneManager.GetComponentManager<T>();
            cm.AddComponent(component, entity.Index);
            SceneManager.ComponentArrays[cm.Index][entity.Index] = entity;
        }

        public virtual void Initialize()
        {
        }

        public virtual void Update(float deltaTime)
        {
            foreach (var system in GetSystems<UpdateSystemAttribute>()) ((IUpdateSystem)system).Execute(deltaTime);
        }

        // TODO: Auto-sort based on dependencies.
        public T StartSystem<T>() where T : Systems.System, new()
        {
            var system = new T();
            var groupType = system.GetType().GetCustomAttribute(typeof(SystemGroupAttribute))?.GetType();
            if (groupType == null)
                groupType = typeof(UpdateSystemAttribute);
            GetSystems((dynamic) Activator.CreateInstance(groupType)).Add(system);
            _allSystems.Add(system);
            return system;
        }

        public void StopSystem<T>() where T : Systems.System
        {
            var groupType = typeof(T).GetCustomAttribute(typeof(SystemGroupAttribute))?.GetType();
            if (groupType == null) return;
            var systemsList = GetSystems((dynamic) Activator.CreateInstance(groupType));
            foreach (var system in systemsList)
            {
                if (system.GetType() != typeof(T)) continue;
                systemsList.Remove(system);
                return;
            }
        }

        public void ToggleSystem<T>() where T : Systems.UpdateSystem, new()
        {
            var groupType = typeof(T).GetCustomAttribute(typeof(SystemGroupAttribute))?.GetType();
            if (groupType == null) return;
            var systemsList = GetSystems((dynamic) Activator.CreateInstance(groupType));
            foreach (var system in systemsList)
            {
                if (system.GetType() != typeof(T)) continue;
                systemsList.Remove(system);
                return;
            }

            StartSystem<T>();
        }

        public void StopSystem<T>(T _) where T : Systems.UpdateSystem
        {
            StopSystem<T>();
        }

        private List<Systems.System> _allSystems = new List<Systems.System>();

        public List<Systems.System> GetAllSystems()
        {
            return _allSystems;
        }

        public static List<Systems.System> GetSystems<T>()
        {
            return PerType<T>.Systems;
        }

        public static List<Systems.System> GetSystems<T>(T _)
        {
            return GetSystems<T>();
        }

        // ReSharper disable once UnusedTypeParameter
        private static class PerType<T>
        {
            // ReSharper disable once StaticMemberInGenericType
            public static readonly List<Systems.System> Systems = new List<Systems.System>();
        }
    }
}