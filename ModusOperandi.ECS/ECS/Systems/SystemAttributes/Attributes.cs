using System;
using JetBrains.Annotations;

namespace ModusOperandi.ECS.Systems.SystemAttributes
{
    [PublicAPI]
    public abstract class SystemGroupAttribute : Attribute
    {
        protected SystemGroupAttribute(params Type[] systemDependencies)
        {
            SystemDependencies = systemDependencies;
        }

        public Type[] SystemDependencies { get; }
    }

    [PublicAPI]
    public class UpdateSystemAttribute : SystemGroupAttribute
    {
        public UpdateSystemAttribute(params Type[] systemDependencies) : base(systemDependencies)
        {
            
        }
    }

    [PublicAPI]
    public class DrawSystemAttribute : SystemGroupAttribute
    {
        public DrawSystemAttribute(params Type[] systemDependencies) : base(systemDependencies)
        {
            
        }
    }
}