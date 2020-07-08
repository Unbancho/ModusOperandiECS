using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModusOperandi.ECS.Components;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.EntityBuilding;
using ModusOperandi.ECS.Systems;
using ModusOperandi.Utils.YAML;
using SFML.Graphics;

namespace ModusOperandi.ECS.Scenes
{
    public class Scene : Drawable
    {
        public Scene()
        {
            Name = GetType().Name;
            EntityManager = new EntityManager();
            ResourcesFolder = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/{Name}";
        }

        public string Name { get; }
        protected string ResourcesFolder { get; set; }
        public List<Entity> Entities { get; } = new List<Entity>();
        protected List<ISystem> Systems => InitializeSystems.Concat(UpdateSystems).Concat(DrawSystems).ToList();
        protected List<ISystem> UpdateSystems { get; } = new List<ISystem>();
        protected List<ISystem> InitializeSystems { get; } = new List<ISystem>();
        protected List<ISystem> DrawSystems { get; } = new List<ISystem>();
        public EntityManager EntityManager { get; set; }


        public void Draw(RenderTarget target, RenderStates states)
        {
            foreach (var system in DrawSystems) system.Execute(0, false, target, states);
        }

        protected void PlaceEntities(string folder = null)
        {
            var files = Directory.GetFiles(folder ?? ResourcesFolder, "*.yaml");
            foreach (var file in files) EntityBuilder.BuildEntity(YAML.Deserialize<object, object>(file), this);
        }

        public void AddComponentToEntity<T>(T component, Entity entity, params object[] componentParams)
        {
            var cm = SceneManager.GetComponentManager<T>();
            cm.Entities[entity.Index] = entity;
            cm.AddComponent(component, entity.Index);
        }

        public virtual void Initialize()
        {
            foreach (var system in InitializeSystems) system.Execute(parallel:false);
        }

        public virtual void Update(float deltaTime)
        {
            foreach (var system in UpdateSystems) system.Execute(deltaTime);
        }
    }
}