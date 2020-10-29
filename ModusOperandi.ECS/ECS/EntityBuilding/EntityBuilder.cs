using System.Collections.Generic;
using JetBrains.Annotations;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.Scenes;

namespace ModusOperandi.ECS.EntityBuilding
{
    [PublicAPI]
    public class EntityBuilder
    {
        public Entity BuildEntity(List<object> components, Scene scene)
        {
            var entity = scene.EntityManager.CreateEntity();
            foreach (var component in components)
            {
                scene.AddComponentToEntity((dynamic) component, entity);
            }

            return entity;
        }
    }
}