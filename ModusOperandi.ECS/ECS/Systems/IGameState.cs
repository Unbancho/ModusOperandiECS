using ModusOperandi.ECS.Scenes;
using SFML.Graphics;

namespace ModusOperandi.ECS.Systems
{
    public interface IGameState
    {
        
    }

    public interface IWindowState : IGameState
    {
        public RenderWindow Window { get; }
    }

    public interface IGameTimeState : IGameState
    {
        public float DeltaTime { get; }
    }
}