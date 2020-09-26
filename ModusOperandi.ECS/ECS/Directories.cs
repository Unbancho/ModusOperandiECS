using System;

namespace ModusOperandi.ECS
{
    public static class Directories
    {
        public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string ResourcesDirectory = $"{BaseDirectory}/Resources/";
        public static string EntitiesDirectory = $"{ResourcesDirectory}/Entities/";
    }
}