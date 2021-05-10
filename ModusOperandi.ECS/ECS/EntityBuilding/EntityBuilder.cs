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
    public static class EntityBuilder
    {
        // TODO: Refactor to allow EntityManager to be private (PlaceEntity?).
        public static Entity BuildEntity(List<object> components, Scene scene)
        {
            var entity = scene.EntityManager.CreateEntity();
            foreach (var component in components)
            {
                Ecs.RegisterComponent(entity, component);
            }

            return entity;
        }

        private static Dictionary<string, List<object>> _entityCache = new();
        public static List<object> LoadEntityComponents(string type)
        {
            if (_entityCache.TryGetValue(type, out var cachedComponents))
            {
                return cachedComponents;
            }
            var components = Yaml.Deserialize<object, List<object>>(Directory
                .GetFiles($"{Directories.EntitiesDirectory}",
                    $"{type}.yaml", SearchOption.AllDirectories)
                .First()).Values.ToList()[0];
            _entityCache[type] = components;
            return components;
        }

        public static bool LoadChildren(string parent, out List<object> lists)
        {
            var dict = Yaml.Deserialize<object, List<object>>(Directory.GetFiles(
                $"{Directories.EntitiesDirectory}",
                $"{parent}.yaml", SearchOption.AllDirectories).First());
            return dict.TryGetValue("children", out lists);
        }
    }
}