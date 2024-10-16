using System;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Util;

namespace Shared.ProceduralGeneration.Island
{
    public static class ConvolutionUtil
    {   
        public static MemoryBuffer1D<float, Stride1D.Dense> Blur(MemoryBuffer1D<float, Stride1D.Dense> input, int kernelSize)
        {
            // Circular blur
            int length = (int)input.Length;
            int width = (int)Math.Sqrt(length);
            int height = width;

            try
            {
                var gpuOutput = GPUtils.accelerator.Allocate1D<float>(length);

                var circularBlurKernel = GPUtils.accelerator.LoadAutoGroupedStreamKernel<
                    Index2D, ArrayView<float>, ArrayView<float>, int, int, int>(CircularBlurKernel);

                circularBlurKernel(new Index2D(width, height), input.View, gpuOutput.View, width, height, kernelSize);
                GPUtils.accelerator.Synchronize();

                return gpuOutput;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GPU execution: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private static void CircularBlurKernel(
            Index2D index,
            ArrayView<float> input,
            ArrayView<float> output,
            int width,
            int height,
            int kernelSize)
        {
            int x = index.X;
            int y = index.Y;
            int radius = kernelSize / 2;

            if (x >= width || y >= height)
                return;

            float sum = 0;
            int count = 0;

            for (int ky = -radius; ky <= radius; ky++)
            {
                for (int kx = -radius; kx <= radius; kx++)
                {
                    // Check if the point is within the circular kernel
                    if (kx * kx + ky * ky <= radius * radius)
                    {
                        int sampleX = x + kx;
                        int sampleY = y + ky;

                        // Check if the sample is within the image bounds
                        if (sampleX >= 0 && sampleX < width && sampleY >= 0 && sampleY < height)
                        {
                            sum += input[sampleY * width + sampleX];
                            count++;
                        }
                    }
                }
            }

            output[y * width + x] = count > 0 ? sum / count : 0;
        }

        public static MemoryBuffer1D<float, Stride1D.Dense> SquareBlur(MemoryBuffer1D<float, Stride1D.Dense> input, int kernelSize)
        {
            int length = (int)input.Length;
            int width = (int)Math.Sqrt(length);
            int height = width;

            try
            {
                // Allocate memory for the output on the GPU
                var gpuOutput = GPUtils.accelerator.Allocate1D<float>(length);

                // Compile and load the kernel
                var convolutionKernel = GPUtils.accelerator.LoadAutoGroupedStreamKernel<
                    Index2D, ArrayView<float>, ArrayView<float>, int, int, int>(SquareKernel);

                // Launch the kernel
                convolutionKernel(new Index2D(width, height), input.View, gpuOutput.View, width, height, kernelSize);
                GPUtils.accelerator.Synchronize();

                return gpuOutput;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GPU execution: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private static void SquareKernel(
            Index2D index,
            ArrayView<float> input,
            ArrayView<float> output,
            int width,
            int height,
            int kernelSize)
        {
            int x = index.X;
            int y = index.Y;
            int halfKernel = kernelSize / 2;

            if (x >= width || y >= height)
                return;

            float sum = 0;
            int count = 0;

            for (int ky = -halfKernel; ky <= halfKernel; ky++)
            {
                for (int kx = -halfKernel; kx <= halfKernel; kx++)
                {
                    int sampleX = x + kx;
                    int sampleY = y + ky;

                    // Check if the sample is within the image bounds
                    if (sampleX >= 0 && sampleX < width && sampleY >= 0 && sampleY < height)
                    {
                        sum += input[sampleY * width + sampleX];
                        count++;
                    }
                }
            }

            output[y * width + x] = count > 0 ? sum / count : 0;
        }

        private static float[] CreateGaussianKernel(int size)
        {
            float[] kernel = new float[size * size];
            float sigma = size / 6f;
            float sum = 0;
            int halfSize = size / 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    float xOffset = x - halfSize;
                    float yOffset = y - halfSize;
                    kernel[index] = (float)Math.Exp(-(xOffset * xOffset + yOffset * yOffset) / (2 * sigma * sigma));
                    sum += kernel[index];
                }
            }

            // Normalize the kernel
            for (int i = 0; i < kernel.Length; i++)
            {
                kernel[i] /= sum;
            }

            return kernel;
        }

        private static MemoryBuffer1D<float, Stride1D.Dense> FallbackCPUConvolution(
            MemoryBuffer1D<float, Stride1D.Dense> input, int kernelSize, int width, int height)
        {
            float[] flatInput = input.GetAsArray1D();
            return GPUtils.accelerator.Allocate1D(flatInput);
            float[,] output = new float[width, height];
            float[] kernel = CreateGaussianKernel(kernelSize);
            int halfKernel = kernelSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
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
                                int kernelIndex = (ky + halfKernel) * kernelSize + (kx + halfKernel);
                                float kernelValue = kernel[kernelIndex];
                                sum += flatInput[sy * width + sx] * kernelValue;
                                weightSum += kernelValue;
                            }
                        }
                    }
                    output[x, y] = weightSum > 0 ? sum / weightSum : 0;
                }
            }

            float[] flatOutput = new float[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flatOutput[y * width + x] = output[x, y];
                }
            }

            return GPUtils.accelerator.Allocate1D(flatOutput);
        }
    }

    internal class SharedMemory<T>
    {
    }
}
