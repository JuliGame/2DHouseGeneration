using System;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
namespace Shared.ProceduralGeneration.Island
{
    public static class CPUHumidity
    {
        private static Context context;
        public static Accelerator accelerator;
        static CPUHumidity()
        {
            try
            {
                context = Context.Create(builder => builder.Default().AllAccelerators());
                accelerator = context.GetPreferredDevice(preferCPU: false)
                    .CreateAccelerator(context);
                Console.WriteLine($"Using accelerator: {accelerator}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing CUDA: {ex.Message}");
                // Fallback to CPU
                context = Context.Create(builder => builder.CPU());
                accelerator = context.GetPreferredDevice(preferCPU: true)
                    .CreateAccelerator(context);
                Console.WriteLine($"Falling back to CPU: {accelerator}");
            }
        }
        public static float[,] GetHumidity(Map map,  float[,] humidityMapSea, float[,] heightMap, bool[,] riverMask, int seed) {
            float[,] humidityMapRiver = Convolution(riverMask, 200);
            humidityMapRiver = MaskUtils.Normalize(humidityMapRiver);
            humidityMapSea = MaskUtils.Normalize(humidityMapSea);
            float[,] humidityMap = MaskUtils.AddMasks(humidityMapRiver, humidityMapSea);
            humidityMap = MaskUtils.AddMasks(humidityMapRiver, humidityMap);
            humidityMap = MaskUtils.Normalize(humidityMap);
            float[,] height = MaskUtils.Multiply(heightMap, .7f);
            humidityMap = MaskUtils.AddMasks(humidityMap, height);
            return humidityMap;
        }        
        public static float[,] Convolution(float[,] input, int kernelSize)
        {
            int width = input.GetLength(0);
            int height = input.GetLength(1);
 
            float[] flatInput = new float[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flatInput[y * width + x] = input[x, y];
                }
            }
            return Convolution(flatInput, kernelSize, width, height);
        }
        public static float[,] Convolution(bool[,] input, int kernelSize)
        {
            int width = input.GetLength(0);
            int height = input.GetLength(1);
            // Prepare input data
            float[] flatInput = new float[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flatInput[y * width + x] = input[x, y] ? 1f : 0f;
                }
            }
            return Convolution(flatInput, kernelSize, width, height);
        }
        public static float[,] Convolution(float[] flatInput, int kernelSize, int width, int height)        {
            float[,] output = new float[width, height];
            float[] kernel = CreateGaussianKernel(kernelSize);
            try
            {
                // Allocate memory on the GPU
                using (var gpuInput = accelerator.Allocate1D(flatInput))
                using (var gpuKernel = accelerator.Allocate1D(kernel))
                using (var gpuOutput = accelerator.Allocate1D<float>(width * height))
                {
                    // Compile and load the kernel
                    var convolutionKernel = accelerator.LoadAutoGroupedStreamKernel<
                        Index2D, ArrayView<float>, ArrayView<float>, ArrayView<float>, int, int, int>(ConvolutionKernel);
                    // Launch the kernel
                    convolutionKernel(new Index2D(width, height), gpuInput.View, gpuKernel.View, gpuOutput.View, width, height, kernelSize);
                    accelerator.Synchronize();
                    // Copy the result back to the CPU
                    float[] flatOutput = gpuOutput.GetAsArray1D();
                    // Convert flat array back to 2D array
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            output[x, y] = flatOutput[y * width + x];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GPU execution: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                
                // Fallback to CPU implementation
                return FallbackCPUConvolution(flatInput, kernelSize, width, height);
            }
            return output;
        }
        private static float[,] FallbackCPUConvolution(float[] flatInput, int kernelSize, int width, int height)
        {
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
            return output;
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
    }
}