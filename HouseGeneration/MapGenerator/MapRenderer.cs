using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Shared.ProceduralGeneration;
using Shared.ProceduralGeneration.Util;
using System;
using System.Collections.Generic;
using System.Threading;

namespace HouseGeneration.MapGeneratorRenderer
{
    public class MapRenderer
    {
        private GraphicsDevice _graphicsDevice;
        private Dictionary<Point, RenderTarget2D> _mapChunks;
        private int ChunkSize = 1024;
        private BasicEffect _basicEffect;
        private VertexPositionColor[] _lineVertices;
        private SpriteFont _font;
        private BasicEffect _textEffect;

        public MapRenderer(GraphicsDevice graphicsDevice, ContentManager content)
        {
            _graphicsDevice = graphicsDevice;
            _mapChunks = new Dictionary<Point, RenderTarget2D>();
            _basicEffect = new BasicEffect(graphicsDevice);
            _basicEffect.VertexColorEnabled = true;
            _basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0, 0, 1);

            _lineVertices = new VertexPositionColor[2];
            _font = content.Load<SpriteFont>("Arial");
            _textEffect = new BasicEffect(graphicsDevice);
            _textEffect.VertexColorEnabled = true;
            _textEffect.TextureEnabled = true;
            _textEffect.Projection = Matrix.CreateOrthographicOffCenter(0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0, 0, 1);
        }

        Thread renderThread;
        public void Update(Map map, Camera camera)
        {
            if (map == null) return;

            // Calculate visible chunks based on camera position and zoom
            Vector2 topLeft = camera.ScreenToWorld(Vector2.Zero);
            Vector2 bottomRight = camera.ScreenToWorld(new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height));

            int startX = (int)Math.Floor(topLeft.X / ChunkSize);
            int startY = (int)Math.Floor(topLeft.Y / ChunkSize);
            int endX = (int)Math.Ceiling(bottomRight.X / ChunkSize);
            int endY = (int)Math.Ceiling(bottomRight.Y / ChunkSize);

            startX = Math.Max(startX, 0);
            startY = Math.Max(startY, 0);
            endX = Math.Min(endX, map.x / ChunkSize);
            endY = Math.Min(endY, map.y / ChunkSize);

            // Generate or update chunks as needed

            if (renderThread == null || !renderThread.IsAlive)
            {
                renderThread = new Thread(() => {
                    for (int x = startX; x <= endX; x++)
                    {
                        for (int y = startY; y <= endY; y++)
                        {
                            Point chunkCoord = new Point(x, y);
                            if (!_mapChunks.ContainsKey(chunkCoord) || map.MapChanged)
                            {
                                RenderChunk(map, chunkCoord);
                            }
                        }
                    }
                    map.MapChanged = false;
                });
                renderThread.Start();
            }


            // Remove chunks that are no longer visible
            List<Point> chunksToRemove = new List<Point>();
            foreach (var chunk in _mapChunks)
            {
                if (chunk.Key.X < startX || chunk.Key.X > endX ||
                    chunk.Key.Y < startY || chunk.Key.Y > endY)
                {
                    chunksToRemove.Add(chunk.Key);
                }
            }

            foreach (var chunkCoord in chunksToRemove)
            {
                _mapChunks[chunkCoord].Dispose();
                _mapChunks.Remove(chunkCoord);
            }
        }

        public void ClearChunks()
        {
            foreach (var chunk in _mapChunks)
            {
                chunk.Value.Dispose();
            }
            _mapChunks.Clear();
        }

        public void Draw(SpriteBatch spriteBatch, Camera _camera)
        {
            foreach (var chunk in _mapChunks)
            {
                Vector2 position = new Vector2(chunk.Key.X * ChunkSize, chunk.Key.Y * ChunkSize);
                Vector2 ScreenSize = new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
                position += ScreenSize / 2;
                position += new Vector2(-1, -1) * ChunkSize;
                // add an offset of 1 tile to the position

                Rectangle destinationRectangle = new Rectangle((int)position.X, (int)position.Y, ChunkSize, ChunkSize);
                if (chunk.Value != null)
                    spriteBatch.Draw(chunk.Value, destinationRectangle, Color.White);
            }

            DrawGizmos(spriteBatch, _camera);
        }

        private void RenderChunk(Map map, Point chunkCoord)
        {
            // Create a new texture for the chunk
            RenderTarget2D chunkTexture = new RenderTarget2D(_graphicsDevice, ChunkSize, ChunkSize);
            Color[] colorData = new Color[ChunkSize * ChunkSize];

            Random random = new Random();
            int i = random.Next(256);
            // Fill the color data
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int x = 0; x < ChunkSize; x++)
                {
                    int mapX = chunkCoord.X * ChunkSize + x;
                    int mapY = chunkCoord.Y * ChunkSize + y;

                    if (mapX < map.x && mapY < map.y && mapX >= 0 && mapY >= 0)
                    {
                        Tile tile = map.GetTile(mapX, mapY);
                        System.Drawing.Color tileColor = map.TextureTypes[tile.TextureIndex].Color;

                        // System.Drawing.Color tileColor = System.Drawing.Color.FromArgb(random.Next(256), i, 255);
                        colorData[y * ChunkSize + x] = new Color(tileColor.R, tileColor.G, tileColor.B);
                    }
                    else
                    {
                        colorData[y * ChunkSize + x] = Color.Transparent;
                    }
                }
            }

            // Set the color data to the texture
            chunkTexture.SetData(colorData);
            // Store the rendered chunk
            _mapChunks[chunkCoord] = chunkTexture;
        }

        private void DrawGizmos(SpriteBatch spriteBatch, Camera _camera)
        {
            spriteBatch.End();

            _basicEffect.World = Matrix.Identity;
            _basicEffect.View = Matrix.Identity;

            // Draw coordinate axes
            DrawLine(Vector2.Zero, new Vector2(100, 0), Color.Red);  // X-axis
            DrawLine(Vector2.Zero, new Vector2(0, 100), Color.Green);  // Y-axis

            // Draw camera position and direction
            Vector2 cameraPos = new Vector2(_graphicsDevice.Viewport.Width / 2, _graphicsDevice.Viewport.Height / 2);
            DrawCircle(cameraPos, 5, Color.Yellow);  // Camera position
            Vector2 directionEnd = cameraPos + new Vector2(MathF.Cos(_camera.Rotation), MathF.Sin(_camera.Rotation)) * 50;
            DrawLine(cameraPos, directionEnd, Color.Blue);  // Camera direction

            // Draw text for chunk coordinates
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());
            foreach (var chunk in _mapChunks)
            {
                Vector2 position = new Vector2(chunk.Key.X * ChunkSize, chunk.Key.Y * ChunkSize);
                Vector2 ScreenSize = new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
                position += ScreenSize / 2;
                position += new Vector2(-1, -1) * ChunkSize;

                //DrawText(spriteBatch, chunk.Key.ToString(), new Vector2(position.X + ChunkSize / 2, position.Y + ChunkSize / 2), Color.White);
            }
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            _lineVertices[0].Position = new Vector3(start, 0);
            _lineVertices[0].Color = color;
            _lineVertices[1].Position = new Vector3(end, 0);
            _lineVertices[1].Color = color;

            foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _lineVertices, 0, 1);
            }
        }

        private void DrawCircle(Vector2 center, float radius, Color color, int segments = 16)
        {
            VertexPositionColor[] circleVertices = new VertexPositionColor[segments + 1];

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * MathHelper.TwoPi / segments;
                Vector2 pos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                circleVertices[i] = new VertexPositionColor(new Vector3(pos, 0), color);
            }

            foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, circleVertices, 0, segments);
            }
        }

        private void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color)
        {
            Vector2 textSize = _font.MeasureString(text);
            Vector2 textPosition = position - textSize / 2;
            spriteBatch.DrawString(_font, text, textPosition, color);
        }
    }
}
