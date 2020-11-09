using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.Scenes;
using ModusOperandi.Utils.YAML;

namespace ModusOperandi.ECS.EntityBuilding
{
    [PublicAPI]
    public class EntityBuilder
    {
        private static Dictionary<string, List<object>> _entityCache = new Dictionary<string, List<object>>();
        
        public Entity BuildEntity(List<object> components, Scene scene)
        {
            var entity = scene.EntityManager.CreateEntity();
            foreach (var component in components)
            {
                scene.AddComponentToEntity((dynamic) component, entity);
            }

            return entity;
        }

        public List<object> LoadEntityComponents(string type)
        {
            if (_entityCache.TryGetValue(type, out var components)) return components;
            components = Yaml.Deserialize<object, List<object>>(Directory
                .GetFiles($"{Directories.EntitiesDirectory}",
                    $"{type}.yaml", SearchOption.AllDirectories).First()).Values.ToList()[0];
            //_entityCache[type] = components;
            return components;
        }
    }
}