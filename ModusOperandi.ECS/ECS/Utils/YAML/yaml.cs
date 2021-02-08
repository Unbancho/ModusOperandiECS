using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using ModusOperandi.ECS.Components;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ModusOperandi.Utils.YAML
{
    [PublicAPI]
    public static class Yaml
    {
        // ReSharper disable once HeapView.ObjectAllocation.Evident
        public static readonly DeserializerBuilder DeserializerBuilder = new();

        private static IDeserializer _deserializer;

        public static void RegisterTagMapping<T>(string tag)
        {
            DeserializerBuilder.WithTagMapping(tag, typeof(T));
        }

        public static void RegisterComponentMappings()
        {
            var componentTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes().Where(type => Attribute.IsDefined(type, typeof(Component))));
            foreach (var componentType in componentTypes)
            {
                DeserializerBuilder
                    .WithTagMapping($"!{componentType.Name.Replace("Component", "").ToLower()}", componentType);
            }
        }

        public static void BuildDeserializer()
        {
            DeserializerBuilder.WithNamingConvention(PascalCaseNamingConvention.Instance);
            _deserializer = DeserializerBuilder.Build();
        }

        public static Dictionary<TK, TV> Deserialize<TK, TV>(string filepath)
        {
            string data;
            using (var sr = new StreamReader(filepath))
            {
                data = sr.ReadToEnd();
            }

            return DeserializeString<TK, TV>(data);
        }

        public static Dictionary<TK, TV> DeserializeString<TK, TV>(string data)
        {
            return _deserializer.Deserialize<Dictionary<TK, TV>>(data);   
        }
    }
}