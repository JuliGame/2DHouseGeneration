using System;
using Shared.ProceduralGeneration;
using Shared.ProceduralGeneration.Util;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Algorithms;

namespace Shared.ProceduralGeneration.Island
{
    public static class GenerateShape
    {
        public static float[,] GenerateIsland(Map map, int seed, bool useCPU)
        {
            int width = map.x;
            int height = map.y;

            // Add new parameters for octaves and lacunarity
            int octaves = 4;
            float lacunarity = 2.0f;
            float persistence = 0.5f;
            float baseScale = 0.005f;

            float[] flatIsland = new float[width * height];

            try
            {
                if (useCPU) {
                    return GenerateIslandCPU(map, seed, octaves, lacunarity, persistence, baseScale);
                }

                Console.WriteLine("Starting GPU island shape generation...");

                using (var gpuIsland = GPUtils.accelerator.Allocate1D<float>(width * height))
                {
                    Console.WriteLine("GPU memory allocated successfully.");

                    var islandShapeKernel = GPUtils.accelerator.LoadAutoGroupedStreamKernel<
                        Index1D, ArrayView<float>, int, int, int, int, float, float, float>(IslandShapeKernel);

                    Console.WriteLine("Kernel loaded successfully.");

                    islandShapeKernel(flatIsland.Length, gpuIsland.View, width, height, seed, octaves, lacunarity, persistence, baseScale);
                    Console.WriteLine("Kernel executed successfully.");

                    GPUtils.accelerator.Synchronize();
                    Console.WriteLine("GPU synchronization complete.");

                    gpuIsland.CopyToCPU(flatIsland);
                    Console.WriteLine("Results copied back to CPU successfully.");
                }

                Console.WriteLine("GPU island shape generation completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GPU execution for GenerateIsland: {ex.Message}");
                Console.WriteLine($"Error occurred at: {ex.StackTrace}");
                Console.WriteLine("Falling back to CPU implementation.");
                return GenerateIslandCPU(map, seed, octaves, lacunarity, persistence, baseScale);
            }

            return GPUtils.Unflatten1DArray(flatIsland, width, height);
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
                    // float value = noiseValue * .5f + .5f;
                    island[x, y] = value;
                }
            }

            return island;
        }

        private static void IslandShapeKernel(
            Index1D index,
            ArrayView<float> island,
            int width,
            int height,
            int seed,
            int octaves,
            float lacunarity,
            float persistence,
            float baseScale)
        {
            int x = index % width;
            int y = index / width;

            float noiseValue = FractalNoise(x, y, seed, octaves, lacunarity, persistence, baseScale);

            float distanceFromBorder = XMath.Min(
                XMath.Min(x, width - 1 - x),
                XMath.Min(y, height - 1 - y)
            ) / (float)XMath.Min(width, height);

            float value = (noiseValue + 0.45f) * distanceFromBorder + distanceFromBorder * 0.5f;
            // float value = noiseValue * .5f + .25f;
            island[index] = value;
        }

        private static float FractalNoise(float x, float y, int seed, int octaves, float lacunarity, float persistence, float baseScale)
        {
            float total = 0;
            float frequency = baseScale;
            float amplitude = 1;
            float maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = x * frequency + seed;
                float sampleY = y * frequency + seed;

                float perlin = GPUPerlin.PerlinNoise(sampleX, sampleY);
                total += perlin * amplitude;

                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / maxValue;
        }
    }
}
