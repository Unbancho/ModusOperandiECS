using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using ModusOperandi.ECS.EntityBuilding;
using ModusOperandi.Utils.YAML;

namespace ModusOperandi.ECS.Scenes
{
    // TODO: Systems in yaml.
    [PublicAPI]
    public static class SceneBuilder
    {
        private static readonly Type[] SceneTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(t => t.GetTypes().Where(type => type.BaseType == typeof(Scene))).ToArray();

        public static Scene BuildScene(Dictionary<object, object> dict)
        {
            var sceneType = SceneTypes.First(type =>
                string.Equals(type.Name, (string) dict["scene"], StringComparison.CurrentCultureIgnoreCase));
            var scene = (Scene) Activator.CreateInstance(sceneType);
            /*
            foreach (var systemName in (dict["systems"] as Dictionary<object, object>)?.Keys)
            {
                
            }
            */
            foreach (var entityDict in (List<object>) dict["entities"])
            {
                var entityName = (entityDict as Dictionary<object, object>)?.Keys.First();
                var argsDict =
                    Yaml.Deserialize<object, object>(Directory
                        .GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}/Resources/Entities/",
                            $"{entityName}.yaml", SearchOption.AllDirectories).First());
                if ((entityDict as Dictionary<object, object>)?[entityName] is List<object> componentDicts)
                    foreach (var componentDict in componentDicts)
                    foreach (var (key, value) in (Dictionary<object, object>) componentDict)
                        argsDict[key] = value;
                EntityBuilder.BuildEntity(argsDict, scene);
            }

            return scene;
        }
    }
}