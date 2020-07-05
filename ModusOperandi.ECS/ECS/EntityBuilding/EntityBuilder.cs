using System;
using System.Collections.Generic;
using System.Linq;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.Scenes;
using ModusOperandi.ECS.Utils.Extensions;

namespace ModusOperandi.ECS.EntityBuilding
{
    public static class EntityBuilder
    {
        public static Entity BuildEntity(Dictionary<object, object> dict, Scene scene)
        {
            var entity = scene.EntityManager.CreateEntity();
            foreach (var component in from componentName in dict.Keys let cname = ((string) componentName).Capitalized() + "Component" let componentType = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(t => t.GetTypes()).First(t => t.Name == cname) where componentType != null select dict[componentName] == null
                ? Activator.CreateInstance(componentType)
                : Activator.CreateInstance(componentType, dict[componentName]))
            {
                scene.AddComponentToEntity((dynamic) component, entity);
            }

            return entity;
        }
    }
}