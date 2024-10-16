using System;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;

namespace Shared.ProceduralGeneration.Island
{
    public static class GPUtils
    {
        public static Context context;
        public static Accelerator accelerator;

        static GPUtils()
        {
            InitializeAccelerator();
        }

        private static void InitializeAccelerator()
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

        // Helper methods

        public static float[] Flatten2DArray(float[,] array)
        {
            int width = array.GetLength(0);
            int height = array.GetLength(1);
            float[] flat = new float[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    flat[y * width + x] = array[x, y];
            return flat;
        }

        public static float[] Flatten2DArray(bool[,] array)
        {
            int width = array.GetLength(0);
            int height = array.GetLength(1);
            float[] flat = new float[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    flat[y * width + x] = array[x, y] ? 1f : 0f;
            return flat;
        }

        public static float[,] Unflatten1DArray(float[] flat, int width, int height)
        {
            float[,] array2D = new float[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    array2D[x, y] = flat[y * width + x];
            return array2D;
        }

        public static float[] CreateGaussianKernel(int size)
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

        public static MemoryBuffer1D<float, Stride1D.Dense> LoadTextureToGPU(float[] textureData)
        {
            return accelerator.Allocate1D(textureData);
        }

        public static void UnloadTextureFromGPU(MemoryBuffer1D<float, Stride1D.Dense> gpuTexture)
        {
            gpuTexture.Dispose();
        }

        // Example method to use the GPU texture in multiple operations
        // public static void ProcessTextureOnGPU(MemoryBuffer1D<float, Stride1D.Dense> gpuTexture, int width, int height)
        // {
        //     // Example kernel launch using the GPU texture
        //     accelerator.Launch(SomeKernel, new Index2D(width, height), gpuTexture.View, width, height);
            
        //     // You can launch multiple kernels here, all using the same gpuTexture
        //     // without transferring it back to the CPU
        // }

        // Example kernel method (define this outside the class as a separate method)
        public static void SomeKernel(Index2D index, ArrayView<float> texture, int width, int height)
        {
            // Kernel operations using the texture data
            // ...
        }

        public static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        public static float Grad(int hash, float x, float y)
        {
            int h = hash & 7;
            float u = h < 4 ? x : y;
            float v = h < 4 ? y : x;
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -2.0f * v : 2.0f * v);
        }

        public static int Hash(int x, int y)
        {
            int hash = (int)((long)x * 1597334677 + (long)y * 3812015801);
            hash = (int)((hash ^ (hash >> 13)) * 1597334677);
            return hash ^ (hash >> 16);
        }

        // Add this helper method
        public static float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }
    }
}
