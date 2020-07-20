using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using ModusOperandi.ECS.Components;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.Scenes;

namespace ModusOperandi.ECS.EntityBuilding
{
    [PublicAPI]
    public static class EntityBuilder
    {
        private static readonly Type[] ComponentTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(t => t.GetTypes().Where(type => Attribute.IsDefined(type, typeof(Component)))).ToArray();

        public static Entity BuildEntity(Dictionary<object, object> dict, Scene scene)
        {
            var entity = scene.EntityManager.CreateEntity();
            foreach (var component in from componentName in dict.Keys
                let cname = $"{componentName}Component".ToLower()
                let componentType = ComponentTypes
                    .FirstOrDefault(t => t.Name.ToLower() == cname)
                where componentType != null
                select dict[componentName] == null
                    ? Activator.CreateInstance(componentType)
                    : Activator.CreateInstance(componentType, ExtractParameters(dict[componentName])))
                scene.AddComponentToEntity((dynamic) component, entity);

            return entity;
        }
        
        private static object[] ExtractParameters(object key)
        {
            var parameters = (key as List<object>)?.ToArray();
            return parameters ?? new object[]{key};
        }
    }
}