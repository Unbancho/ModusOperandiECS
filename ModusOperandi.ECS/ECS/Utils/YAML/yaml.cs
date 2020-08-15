using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace ModusOperandi.Utils.YAML
{
    [PublicAPI]
    public static class Yaml
    {
        // ReSharper disable once HeapView.ObjectAllocation.Evident
        private static readonly DeserializerBuilder DeserializerBuilder = new DeserializerBuilder();

        private static IDeserializer _deserializer;

        public static void RegisterTagMapping<T>(string tag)
        {
            DeserializerBuilder.WithTagMapping(tag, typeof(T));
        }

        public static void BuildDeserializer()
        {
            _deserializer = DeserializerBuilder.Build();
        }

        public static Dictionary<TK, TV> Deserialize<TK, TV>(string filepath)
        {
            string data;
            // ReSharper disable once HeapView.ObjectAllocation.Evident
            using (var sr = new StreamReader(filepath))
            {
                data = sr.ReadToEnd();
            }

            return _deserializer.Deserialize<Dictionary<TK, TV>>(data);
        }
    }
}