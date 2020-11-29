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
        // TODO: Refactor to allow EntityManager to be private (PlaceEntity?), and to allow building entities without a scene.
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
            var components = Yaml.Deserialize<object, List<object>>(Directory
                .GetFiles($"{Directories.EntitiesDirectory}",
                    $"{type}.yaml", SearchOption.AllDirectories)
                .First()).Values.ToList()[0];
            return components;
        }
    }
}