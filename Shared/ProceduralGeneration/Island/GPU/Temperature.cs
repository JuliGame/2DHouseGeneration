using System;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Algorithms;

namespace Shared.ProceduralGeneration.Island
{
    public static class TemperatureCalculator
    {
        public static float[,] GetTemperature(Map map, int seed, MemoryBuffer1D<float, Stride1D.Dense> gpuHeightmap, bool useCPU)
        {
            int width = map.x;
            int height = map.y;
            float[] flatNoiseMap = new float[width * height];

            try
            {
                if (useCPU) {
                    return GetTemperatureCPU(map, seed, GPUtils.Unflatten1DArray(gpuHeightmap.GetAsArray1D(), width, height));
                }

                Console.WriteLine("Starting GPU temperature generation...");

                using (var gpuNoiseMap = GPUtils.accelerator.Allocate1D<float>(width * height))
                {
                    Console.WriteLine("GPU memory allocated successfully.");

                    var temperatureKernel = GPUtils.accelerator.LoadAutoGroupedStreamKernel<
                        Index1D, ArrayView<float>, ArrayView<float>, int, int, int>(TemperatureKernel);

                    Console.WriteLine("Kernel loaded successfully.");

                    temperatureKernel(flatNoiseMap.Length, gpuNoiseMap.View, gpuHeightmap.View, width, height, seed);
                    Console.WriteLine("Kernel executed successfully.");

                    GPUtils.accelerator.Synchronize();
                    Console.WriteLine("GPU synchronization complete.");

                    gpuNoiseMap.CopyToCPU(flatNoiseMap);
                    Console.WriteLine("Results copied back to CPU successfully.");
                }

                Console.WriteLine("GPU temperature generation completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GPU execution for GetTemperature: {ex.Message}");
                Console.WriteLine($"Error occurred at: {ex.StackTrace}");
                Console.WriteLine("Falling back to CPU implementation.");
                return GetTemperatureCPU(map, seed, GPUtils.Unflatten1DArray(gpuHeightmap.GetAsArray1D(), width, height));
            }

            return GPUtils.Unflatten1DArray(flatNoiseMap, width, height);
        }

        private static float[,] GetTemperatureCPU(Map map, int seed, float[,] heightmap)
        {
            int width = map.x;
            int height = map.y;
            float[,] temperatureMap = new float[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noise = SimplifiedNoise(x, y, seed);
                    temperatureMap[x, y] = noise * heightmap[x, y];
                }
            }

            return temperatureMap;
        }

        private static void TemperatureKernel(
            Index1D index,
            ArrayView<float> temperatureMap,
            ArrayView<float> heightmap,
            int width,
            int height,
            int seed)
        {
            int x = index % width;
            int y = index / width;

            float noise = FractalNoise(x, y, seed, 6, 2.0f, 0.5f);

            float heightInfluence = heightmap[index] > .5f ? 1f : heightmap[index];
            float noiseInfluence = 1 - heightInfluence;

            // Simple blur for noise influence
            float blurredNoiseInfluence = noiseInfluence;
            int blurRadius = 10;
            int count = 1;

            if (heightmap[index] > .4f) {
                for (int offsetY = -blurRadius; offsetY <= blurRadius; offsetY++)
                {
                    for (int offsetX = -blurRadius; offsetX <= blurRadius; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0) continue;

                        int neighborX = x + offsetX;
                        int neighborY = y + offsetY;

                        if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                        {
                            int neighborIndex = neighborY * width + neighborX;
                            float neighborHeightInfluence = heightmap[neighborIndex] > .5f ? 1f : heightmap[neighborIndex];
                            float neighborNoiseInfluence = 1 - neighborHeightInfluence;
                            blurredNoiseInfluence += neighborNoiseInfluence;
                            count++;
                        }
                    }
                }

                blurredNoiseInfluence /= count;
            }

            temperatureMap[index] = noise * blurredNoiseInfluence;
        }

        private static float SimplifiedNoise(int x, int y, int seed)
        {
            return FractalNoise(x, y, seed, 6, 2.0f, 0.5f);
        }

        private static float FractalNoise(float x, float y, int seed, int octaves, float lacunarity, float persistence)
        {
            float total = 0;
            float frequency = 0.005f;
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
