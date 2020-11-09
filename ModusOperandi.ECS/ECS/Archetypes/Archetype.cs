using System;
using JetBrains.Annotations;
using ModusOperandi.ECS.Components;

namespace ModusOperandi.ECS.Archetypes
{
    [PublicAPI]
    public struct Archetype
    {
        public ulong Signature;

        public Archetype(params Type[] componentTypes)
        {
            Signature = 0u;
            foreach (var type in componentTypes)
            {
                Signature |= ComponentExtensions.GetSignature((dynamic) Activator.CreateInstance(type));
            }
        }
    }
}