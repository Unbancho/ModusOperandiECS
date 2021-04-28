using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security;
using JetBrains.Annotations;
using SFML;
using SFML.Graphics;
using SFML.System;

namespace ModusOperandi.Rendering
{
    [PublicAPI]
    public class SpriteBatch : Drawable
    {
        private struct QueueItem
        {
            public uint Count;
            public IntPtr Texture;
        }

        // ReSharper disable once HeapView.ObjectAllocation.Evident
        private readonly List<QueueItem> _textures = new();

        private readonly int _max;

        private int Count { get; set; }

        public SpriteBatch(int maxCapacity = 100000)
        {
            _max = maxCapacity * 4;
        }

        // ReSharper disable once HeapView.ObjectAllocation.Evident
        private Vertex[] _vertices = new Vertex[100 * 4];
        private IntPtr _activeTexture;
        private uint _queueCount;

        public void Begin()
        {
            Count = 0;
            _textures.Clear();
            _activeTexture = IntPtr.Zero;
            _texts.Clear();
        }

        public void End()
        {
            Enqueue();
        }

        private void Enqueue()
        {
            if (_queueCount > 0)
                _textures.Add(new()
                {
                    Texture = _activeTexture,
                    Count = _queueCount
                });
            _queueCount = 0;
        }

        private int Create(IntPtr texture)
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
                else throw new("Too many items");
            }

            _queueCount += 4;
            return 4 * Count++;
        }

        public void Draw(Sprite sprite)
        {
            Draw(sprite.Texture?.CPointer ?? IntPtr.Zero, new(sprite.Position.X, sprite.Position.Y), sprite.TextureRect, sprite.Color, new(sprite.Scale.X, sprite.Scale.Y), new(sprite.Origin.X, sprite.Origin.Y),
                 sprite.Rotation);
        }

        public unsafe void Draw(IntPtr texture, Vector2 position, IntRect rec, Color color, Vector2 scale,
                                Vector2 origin, float rotation = 0)
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

            fixed (Vertex* fixedPtr = _vertices)
            {
                var ptr = fixedPtr + index;

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

        public void Draw(Text text)
        {
            _texts.Add(text);
        }

        private List<Text> _texts = new();
        public void Draw(RenderTarget target, RenderStates states)
        {
            var windowPtr = ((ObjectBase) target).CPointer;
            var marshaledStates = new MarshalData
            {
                blendMode = states.BlendMode,
                shader = states.Shader?.CPointer ?? IntPtr.Zero,
                transform = states.Transform
            };
            uint index = 0;
            foreach (var item in _textures)
            {
                marshaledStates.texture = item.Texture;
                unsafe
                {
                    fixed (Vertex* vertexPtr = _vertices)
                    {
                        sfRenderWindow_drawPrimitives(windowPtr, vertexPtr + index, item.Count, PrimitiveType.Quads, ref marshaledStates);
                    }
                }
                
                index += item.Count;
            }

            foreach (var text in _texts)
            {
                text.Draw(target, states);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal ref struct MarshalData
        {
            public BlendMode blendMode;
            public Transform transform;
            public IntPtr texture;
            public IntPtr shader;
        }
        
        [DllImport(CSFML.graphics, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern unsafe void sfRenderWindow_drawPrimitives(IntPtr cPointer, Vertex* vertexPtr, uint vertexCount, PrimitiveType type, ref MarshalData renderStates);
        
        public unsafe void Draw(IntPtr texture, FloatRect rec, IntRect src, Color color)
        {
            var index = Create(texture);

            fixed (Vertex* fixedPtr = _vertices)
            {
                var ptr = fixedPtr + index;

                ptr->Position.X = rec.Left;
                ptr->Position.Y = rec.Top;
                ptr->TexCoords.X = src.Left;
                ptr->TexCoords.Y = src.Top;
                ptr->Color = color;
                ptr++;

                ptr->Position.X = rec.Left + rec.Width;
                ptr->Position.Y = rec.Top;
                ptr->TexCoords.X = src.Left + src.Width;
                ptr->TexCoords.Y = src.Top;
                ptr->Color = color;
                ptr++;

                ptr->Position.X = rec.Left + rec.Width;
                ptr->Position.Y = rec.Top + rec.Height;
                ptr->TexCoords.X = src.Left + src.Width;
                ptr->TexCoords.Y = src.Top + src.Height;
                ptr->Color = color;
                ptr++;

                ptr->Position.X = rec.Left;
                ptr->Position.Y = rec.Top + rec.Height;
                ptr->TexCoords.X = src.Left;
                ptr->TexCoords.Y = src.Top + src.Height;
                ptr->Color = color;
            }
        }

        public void Draw(Texture texture, FloatRect rec, Color color)
        {
            int width = 1, height = 1;
            if (texture != null)
            {
                width = (int)texture.Size.X;
                height = (int)texture.Size.Y;
            }
            Draw(texture!.CPointer, rec, new(0, 0, width, height), color);
        }

        public void Draw(Texture texture, Vector2 pos, Color color)
        {
            var width = (int)texture.Size.X;
            var height = (int)texture.Size.Y;
            Draw(texture.CPointer, new(pos.X, pos.Y, width, height), new(0, 0, width, height), color);
        }
    }
}