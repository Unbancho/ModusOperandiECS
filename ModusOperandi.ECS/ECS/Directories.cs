using System;
using JetBrains.Annotations;

namespace ModusOperandi.ECS
{
    [PublicAPI]
    public static class Directories
    {
        public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string ResourcesDirectory = $"{BaseDirectory}/Resources/";
        public static readonly string EntitiesDirectory = $"{ResourcesDirectory}/Entities/";
    }
}