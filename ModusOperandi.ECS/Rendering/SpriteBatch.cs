using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;

namespace ModusOperandi.Rendering
{
    public class SpriteBatch : Drawable
    {
        private struct QueueItem
        {
            public uint Count;
            public Texture Texture;
        }

        private readonly List<QueueItem> _textures = new List<QueueItem>();

        private readonly int _max;

        private int Count { get; set; }

        public SpriteBatch(int maxCapacity = 100000)
        {
            _max = maxCapacity * 4;
        }

        private Vertex[] _vertices = new Vertex[100 * 4];
        private Texture _activeTexture;
        private uint _queueCount;

        public void Begin()
        {
            Count = 0;
            _textures.Clear();
            _activeTexture = null;
        }

        public void End()
        {
            Enqueue();
        }

        private void Enqueue()
        {
            if (_queueCount > 0)
                _textures.Add(new QueueItem
                {
                    Texture = _activeTexture,
                    Count = _queueCount
                });
            _queueCount = 0;
        }

        private int Create(Texture texture)
        {
            if (texture != _activeTexture)
            {
                Enqueue();
                _activeTexture = texture;
            }

            if (Count >= _vertices.Length / 4)
            {
                if (_vertices.Length < _max)
                    Array.Resize(ref _vertices, Math.Min(_vertices.Length * 2, _max));
                else throw new Exception("Too many items");
            }

            _queueCount += 4;
            return 4 * Count++;
        }

        public void Draw(Sprite sprite)
        {
            Draw(sprite.Texture, sprite.Position, sprite.TextureRect, sprite.Color, sprite.Scale, sprite.Origin,
                 sprite.Rotation);
        }

        private unsafe void Draw(Texture texture, Vector2f position, IntRect rec, Color color, Vector2f scale,
                                Vector2f origin, float rotation = 0)
        {

            var index = Create(texture);
            float sin=0, cos=1;

            if (rotation > 0 || rotation < 0)
            {
                rotation = (float)(rotation*Math.PI/180);
                sin = (float)Math.Sin(rotation);
                cos = (float)Math.Cos(rotation);
            }

            var pX = -origin.X * scale.X;
            var pY = -origin.Y * scale.Y;
            scale.X *= rec.Width;
            scale.Y *= rec.Height;

            fixed (Vertex* fptr = _vertices)
            {
                var ptr = fptr + index;

                ptr->Position.X = pX * cos - pY * sin + position.X;
                ptr->Position.Y = pX * sin + pY * cos + position.Y;
                ptr->TexCoords.X = rec.Left;
                ptr->TexCoords.Y = rec.Top;
                ptr->Color = color;
                ptr++;

                pX += scale.X;
                ptr->Position.X = pX * cos - pY * sin + position.X;
                ptr->Position.Y = pX * sin + pY * cos + position.Y;
                ptr->TexCoords.X = rec.Left + rec.Width;
                ptr->TexCoords.Y = rec.Top;
                ptr->Color = color;
                ptr++;

                pY += scale.Y;
                ptr->Position.X = pX * cos - pY * sin + position.X;
                ptr->Position.Y = pX * sin + pY * cos + position.Y;
                ptr->TexCoords.X = rec.Left + rec.Width;
                ptr->TexCoords.Y = rec.Top + rec.Height;
                ptr->Color = color;
                ptr++;

                pX -= scale.X;
                ptr->Position.X = pX * cos - pY * sin + position.X;
                ptr->Position.Y = pX * sin + pY * cos + position.Y;
                ptr->TexCoords.X = rec.Left;
                ptr->TexCoords.Y = rec.Top + rec.Height;
                ptr->Color = color;
            }
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            uint index = 0;
            foreach (var item in _textures)
            {
                states.Texture = item.Texture;
                target.Draw(_vertices, index, item.Count, PrimitiveType.Quads, states);
                index += item.Count;
            }
        }
    }
}