using System;
using JetBrains.Annotations;

namespace ModusOperandi.ECS
{
    [PublicAPI]
    public static class Directories
    {
        public static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;
        public static string ResourcesDirectory => $"{BaseDirectory}/Resources/";
        public static string EntitiesDirectory => $"{ResourcesDirectory}/Entities/";
    }
}