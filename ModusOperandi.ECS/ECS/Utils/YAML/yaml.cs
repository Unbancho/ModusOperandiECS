using System.Collections.Generic;
using System.IO;
using SFML.Graphics;
using SFML.System;
using YamlDotNet.Serialization;

namespace ModusOperandi.Utils.YAML
{
    public static class YAML
    {
        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithTagMapping("!vector2f", typeof(Vector2f))
            .WithTagMapping("!text", typeof(Text))
            .WithTagMapping("!color", typeof(Color))
            .WithTagMapping("!style", typeof(Text.Styles))
            .Build();

        public static Dictionary<K, V> Deserialize<K, V>(string filepath)
        {
            string data;
            using (var sr = new StreamReader(filepath))
            {
                data = sr.ReadToEnd();
            }

            return Deserializer.Deserialize<Dictionary<K, V>>(data);
        }
    }
}