using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using ModusOperandi.ECS.Archetypes;
using ModusOperandi.ECS.Scenes;
using ModusOperandi.Rendering;

namespace ModusOperandi.ECS.Systems.SystemInterfaces
{
    [PublicAPI]
    public interface ISystem
    {
        Scene Scene { get; set; }
        List<ISystem> ComplementarySystems { get; }
        bool Parallel { get; set; }
    }

    [PublicAPI]
    public interface ISystem<in T> : ISystem where T : IGameState
    {
        void Run(T gameState);
    }

    [PublicAPI]
    public interface IEntitySystem : ISystem
    {
        Archetype[] Archetypes { get; }
    }

    [PublicAPI]
    public interface IComponentSystem<T> : ISystem where T : struct
    {
        Span<T> Components { get; }
    }

    [PublicAPI]
    public interface IUpdateSystem<in T> : ISystem<T> where T : IGameTimeState
    {
        void Execute(T gameState);
    }

    [PublicAPI]
    public interface IDrawSystem : ISystem
    {
        void Draw(SpriteBatch spriteBatch);
    }
    
    [PublicAPI]
    public interface IEmitSystem<in T> : ISystem where T : IEntityEvent
    {
        void Emit(T e);
    }

    [PublicAPI]
    public interface IListenSystem<T> : ISystem where T : IEntityEvent
    {
        ConcurrentStack<T> Events { get; }
    }
}