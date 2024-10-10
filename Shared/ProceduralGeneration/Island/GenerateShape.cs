using System;
using Shared.ProceduralGeneration;
using Shared.ProceduralGeneration.Util;

namespace Shared.ProceduralGeneration.Island
{
    public static class GenerateShape
    {
        public static void GenerateIsland(Map map, int seed)
        {
            Random random = new Random(seed);
            PerlinNoise noise = new PerlinNoise(seed);

            int width = map.x;
            int height = map.y;

            // Add new parameters for octaves and lacunarity
            int octaves = 4;
            float lacunarity = 2.0f;
            float persistence = 0.5f;
            float baseScale = 0.005f;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Generate fractal Brownian motion (fBm) noise
                    float noiseValue = 0f;
                    float frequency = baseScale;
                    float amplitude = 1f;
                    float maxValue = 0f;

                    for (int i = 0; i < octaves; i++)
                    {
                        noiseValue += (float)noise.Noise(x * frequency, y * frequency) * amplitude;
                        maxValue += amplitude;
                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }

                    // Normalize the noise value
                    noiseValue /= maxValue;

                    float distanceFromBorder = Math.Min(
                        Math.Min(x, width - 1 - x),
                        Math.Min(y, height - 1 - y)
                    ) / (float)Math.Min(width, height);

                    float value = (noiseValue * 0.5f + 0.5f) * distanceFromBorder + distanceFromBorder * 0.5f;

                    if (value > 0.1f)
                    {
                        // Land
                        int shade = random.Next(100, 200);
                        map.Paint(new Texture("Grass", System.Drawing.Color.FromArgb(0, shade, 0)), x, y);
                    }
                    else
                    {
                        // Ocean
                        int shade = random.Next(50, 150);
                        map.Paint(new Texture("Water", System.Drawing.Color.FromArgb(0, 0, shade)), x, y);
                    }
                }
            }
        }
    }
}