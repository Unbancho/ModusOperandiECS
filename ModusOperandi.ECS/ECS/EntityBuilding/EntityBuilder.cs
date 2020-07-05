using System;
using System.Collections.Generic;
using ModusOperandi.ECS.Scenes;
using ModusOperandi.ECS.Components;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.Utils.Extensions;

using System.Linq;

namespace ModusOperandi.ECS.EntityBuilding
{
    public static class EntityBuilder
    {
        public static Entity BuildEntity(Dictionary<object, object> dict, Scene scene)
        {
            var entity = scene.EntityManager.CreateEntity();
            foreach (var componentName in dict.Keys)
            {
                var cname = ((string) componentName).Capitalized() + "Component";
                var componentType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(t => t.GetTypes())
                    .Where(t => t.Name == cname).First();
                if (componentType == null)
                    continue;
                var component = dict[componentName] == null
                    ? Activator.CreateInstance(componentType)
                    : Activator.CreateInstance(componentType, dict[componentName]);
                scene.AddComponentToEntity(component, entity);
            }
            return entity;
        }
    }
}