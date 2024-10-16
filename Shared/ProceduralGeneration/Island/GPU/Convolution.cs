using System;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Util;

namespace Shared.ProceduralGeneration.Island
{
    public static class ConvolutionUtil
    {   

        public static MemoryBuffer1D<float, Stride1D.Dense> Convolution(MemoryBuffer1D<float, Stride1D.Dense> input, int kernelSize)
        {
            int length = (int)input.Length;
            int width = (int)Math.Sqrt(length);
            int height = width;

            // Create Gaussian kernel
            float[] kernel = CreateGaussianKernel(kernelSize);
            
            try
            {
                // Allocate memory for the kernel and output on the GPU
                var gpuKernel = GPUtils.accelerator.Allocate1D(kernel);
                var gpuOutput = GPUtils.accelerator.Allocate1D<float>(length);

                // Compile and load the kernel
                var convolutionKernel = GPUtils.accelerator.LoadAutoGroupedStreamKernel<
                    Index2D, ArrayView<float>, ArrayView<float>, ArrayView<float>, int, int, int>(ConvolutionKernel);

                // Launch the kernel
                convolutionKernel(new Index2D(width, height), input.View, gpuKernel.View, gpuOutput.View, width, height, kernelSize);
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

        private static void ConvolutionKernel(
            Index2D index,
            ArrayView<float> input,
            ArrayView<float> kernel,
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
            float weightSum = 0;

            for (int ky = -halfKernel; ky <= halfKernel; ky++)
            {
                for (int kx = -halfKernel; kx <= halfKernel; kx++)
                {
                    int sampleX = x + kx;
                    int sampleY = y + ky;

                    if (sampleX >= 0 && sampleX < width && sampleY >= 0 && sampleY < height)
                    {
                        int kernelIndex = (ky + halfKernel) * kernelSize + (kx + halfKernel);
                        float kernelValue = kernel[kernelIndex];
                        sum += input[sampleY * width + sampleX] * kernelValue;
                        weightSum += kernelValue;
                    }
                }
            }

            output[y * width + x] = weightSum > 0 ? sum / weightSum : 0;
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
