using System;
using ModusOperandi.ECS.Components;

namespace ModusOperandi.ECS.Archetypes
{
    public class Archetype
    {
        private ulong _all;
        private ulong _one;
        private ulong _none;
        public ulong Signature => _all & _one & _none;

        public void MatchAllOf(params Type[] componentTypes)
        {
            foreach (var componentType in componentTypes)
            {
                _all &= GetSignatureFromGenericType((dynamic) Activator.CreateInstance(componentType));
            }
        }
        
        public void MatchOneOf(params Type[] componentTypes)
        {
            foreach (var componentType in componentTypes)
            {
                _one |= GetSignatureFromGenericType((dynamic) Activator.CreateInstance(componentType));
            }
        }
        
        public void MatchNoneOf(params Type[] componentTypes)
        {
            foreach (var componentType in componentTypes)
            {
                _none &= ~GetSignatureFromGenericType((dynamic) Activator.CreateInstance(componentType));
            }
        }

        private static ulong GetSignatureFromGenericType<T>(T _) where T: unmanaged
        {
            return ComponentManager<T>.Signature;
        }
    }
}