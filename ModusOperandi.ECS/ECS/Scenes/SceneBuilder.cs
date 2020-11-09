using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using ModusOperandi.ECS.EntityBuilding;

namespace ModusOperandi.ECS.Scenes
{
    // TODO: Systems in yaml.
    [PublicAPI]
    public class SceneBuilder
    {
        private readonly Type[] _sceneTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(t => t.GetTypes().Where(type => type.BaseType == typeof(Scene))).ToArray();
        
        public Scene BuildScene(Dictionary<object, object> dict)
        {
            var sceneType = _sceneTypes.First(type =>
                string.Equals(type.Name, (string) dict["scene"], StringComparison.CurrentCultureIgnoreCase));
            var scene = (Scene) Activator.CreateInstance(sceneType);
            /* foreach (var systemName in (dict["systems"] as Dictionary<object, object>)?.Keys)
            {
                
            }*/
            var entityBuilder = new EntityBuilder();
            foreach (var entityDict in (List<object>) dict["entities"])
            {
                var entityName = (string) (entityDict as Dictionary<object, object>)?.Keys.First();
                var components = (List<object>) (entityDict as Dictionary<object, object>)?[entityName];
                var defaultComponents = entityBuilder.LoadEntityComponents(entityName);
                entityBuilder.BuildEntity(
                    MergeComponentLists(defaultComponents, components), scene);
            }

            return scene;
        }

        private List<object> MergeComponentLists(List<object> defaultComponents, List<object> updatedComponents)
        {
            var dict = new Dictionary<string, object>();
            foreach (var defaultComponent in defaultComponents)
            {
                dict[((dynamic)defaultComponent).GetType().Name] = defaultComponent;
            }

            foreach (var updatedComponent in updatedComponents)
            {
                if(!dict.TryGetValue(updatedComponent.GetType().Name, out dynamic d))
                {
                    dict[updatedComponent.GetType().Name] = updatedComponent;
                }
                else
                {
                    dict[updatedComponent.GetType().Name] = MergeComponents(ref d, (dynamic) updatedComponent);
                }
            }

            return dict.Values.ToList();
        }

        private T MergeComponents<T>(ref T @default, T updated)
        {
            static void OverWriteMembers(ref T @default, T updated, IEnumerable<dynamic> members)
            {
                foreach (var member in members)
                {
                    var defaultValue = member.GetValue(@default);
                    var updatedValue = member.GetValue(updated);
                    if (updatedValue != defaultValue)
                    {
                        member.SetValue(@default, updatedValue);
                    }
                }
            }

            var type = @default.GetType();
            OverWriteMembers(ref @default, updated, type.GetFields());
            OverWriteMembers(ref @default, updated, type.GetProperties());
            return @default;
        }
    }
}