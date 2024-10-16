using System;
using ILGPU;
using ILGPU.Runtime;

namespace Shared.ProceduralGeneration.Island
{
    public static class HumidityCalculator
    {
        public static float[,] GetHumidity(Map map, MemoryBuffer1D<float, Stride1D.Dense> humidityMapSea, MemoryBuffer1D<float, Stride1D.Dense> heightMap, MemoryBuffer1D<float, Stride1D.Dense> riverMask, bool[,] riverMaskBool, int seed, bool useCPU)
        {
            int width = map.x;
            int height = map.y;


            float[] flatHumidityMap = new float[width * height];

            try
            {
                if (useCPU) {
                    return CPUHumidity.GetHumidity(map, GPUtils.Unflatten1DArray(humidityMapSea.GetAsArray1D(), width, height), GPUtils.Unflatten1DArray(heightMap.GetAsArray1D(), width, height), riverMaskBool, seed);
                }

                using (var gpuHumidityMap = GPUtils.accelerator.Allocate1D<float>(width * height))
                {
                    var humidityKernel = GPUtils.accelerator.LoadAutoGroupedStreamKernel<
                        Index2D, ArrayView<float>, ArrayView<float>, ArrayView<float>, ArrayView<float>, int, int>(HumidityKernel);

                    humidityKernel(new Index2D(width, height), humidityMapSea.View, heightMap.View, riverMask.View, gpuHumidityMap.View, width, height);
                    GPUtils.accelerator.Synchronize();

                    flatHumidityMap = gpuHumidityMap.GetAsArray1D();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GPU execution for GetHumidity: {ex.Message}");
                // Implement a CPU fallback if needed
            }

            return GPUtils.Unflatten1DArray(flatHumidityMap, width, height);
        }

        private static void HumidityKernel(
            Index2D index,
            ArrayView<float> humidityMapSea,
            ArrayView<float> heightMap,
            ArrayView<float> riverMask,
            ArrayView<float> output,
            int width,
            int height)
        {
            int x = index.X;
            int y = index.Y;
            if (x >= width || y >= height) return;

            int i = y * width + x;

            float river = riverMask[i] * 2f;
            float sea = humidityMapSea[i] * 1f;
            float humidity = (river + sea * .15f);
            float h = heightMap[i] * 0.35f;
            output[i] = humidity + h;
        }

        private static float Convolution1D(ArrayView<float> input, int width, int height, int x, int y, int kernelSize)
        {
            int halfKernel = kernelSize / 2;
            float sum = 0;
            float weightSum = 0;

            for (int ky = -halfKernel; ky <= halfKernel; ky++)
            {
                for (int kx = -halfKernel; kx <= halfKernel; kx++)
                {
                    int sx = x + kx;
                    int sy = y + ky;
                    if (sx >= 0 && sx < width && sy >= 0 && sy < height)
                    {
                        float kernelValue = 1f / (kernelSize * kernelSize);
                        sum += input[sy * width + sx] * kernelValue;
                        weightSum += kernelValue;
                    }
                }
            }

            return weightSum > 0 ? sum / weightSum : 0;
        }

        // ... (other humidity-related methods)
    }
}