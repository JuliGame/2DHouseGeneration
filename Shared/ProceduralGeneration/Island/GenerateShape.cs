using System;
using Shared.ProceduralGeneration;
using Shared.ProceduralGeneration.Util;

namespace Shared.ProceduralGeneration.Island
{
    public static class GenerateShape
    {
        public static float[,] GenerateIsland(Map map, int seed)
        {
            int width = map.x;
            int height = map.y;

            int octaves = 4;
            float lacunarity = 2.0f;
            float persistence = 0.5f;
            float baseScale = 0.005f;

            return GenerateIslandCPU(map, seed, octaves, lacunarity, persistence, baseScale);
        }

        private static float[,] GenerateIslandCPU(Map map, int seed, int octaves, float lacunarity, float persistence, float baseScale)
        {
            PerlinNoise noise = new PerlinNoise(seed);

            int width = map.x;
            int height = map.y;

            float[,] island = new float[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float noiseValue = FractalNoise(x, y, noise, octaves, lacunarity, persistence, baseScale);

                    float distanceFromBorder = Math.Min(
                        Math.Min(x, width - 1 - x),
                        Math.Min(y, height - 1 - y)
                    ) / (float)Math.Min(width, height);

                    float value = (noiseValue * 0.5f + 0.5f) * distanceFromBorder + distanceFromBorder * 0.5f;
                    island[x, y] = value;
                }
            }

            return island;
        }

        private static float FractalNoise(float x, float y, PerlinNoise noise, int octaves, float lacunarity, float persistence, float baseScale)
        {
            float total = 0;
            float frequency = baseScale;
            float amplitude = 1;
            float maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = x * frequency;
                float sampleY = y * frequency;

                float perlin = (float)noise.Noise(sampleX, sampleY);
                total += perlin * amplitude;

                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / maxValue;
        }
    }
}