using System;
using System.Threading.Tasks;

namespace Shared.ProceduralGeneration.Island
{
    public static class ConvolutionUtil
    {   
        public static float[,] Blur(bool[,] input, int kernelSize)
        {
            int width = input.GetLength(0);
            int height = input.GetLength(1);
            float[,] input1D = new float[width, height];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    input1D[i, j] = input[i, j] ? 1f : 0f;

            return Blur(input1D, kernelSize);
        }

        public static float[,] Blur(float[,] input, int kernelSize)
        {
            // Determine the scale factor based on kernel size
            int scaleFactor = Math.Max(1, kernelSize / 8);
            int width = input.GetLength(0);
            int height = input.GetLength(1);
            float[] input1D = new float[width * height];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    input1D[j * width + i] = input[i, j];

            float[] output1D = ApplyCircularBlurWithScaling(input1D, width, height, kernelSize, scaleFactor);
            float[,] output = new float[width, height];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    output[i, j] = output1D[j * width + i];
            return output;
        }

        private static float[] ApplyCircularBlurWithScaling(float[] input, int width, int height, int kernelSize, int scaleFactor)
        {
            // Downscale
            int smallWidth = width / scaleFactor;
            int smallHeight = height / scaleFactor;
            float[] smallInput = Downscale(input, width, height, smallWidth, smallHeight);

            // Apply blur on smaller image
            int smallKernelSize = Math.Max(3, kernelSize / scaleFactor);
            float[] smallOutput = ApplyCircularBlur(smallInput, smallWidth, smallHeight, smallKernelSize);

            // Upscale
            return Upscale(smallOutput, smallWidth, smallHeight, width, height);
        }

        private static float[] Downscale(float[] input, int width, int height, int newWidth, int newHeight)
        {
            float[] output = new float[newWidth * newHeight];
            float scaleX = (float)width / newWidth;
            float scaleY = (float)height / newHeight;

            Parallel.For(0, newHeight, y =>
            {
                for (int x = 0; x < newWidth; x++)
                {
                    int srcX = (int)(x * scaleX);
                    int srcY = (int)(y * scaleY);
                    output[y * newWidth + x] = input[srcY * width + srcX];
                }
            });

            return output;
        }

        private static float[] Upscale(float[] input, int width, int height, int newWidth, int newHeight)
        {
            float[] output = new float[newWidth * newHeight];
            float scaleX = (float)width / newWidth;
            float scaleY = (float)height / newHeight;

            Parallel.For(0, newHeight, y =>
            {
                for (int x = 0; x < newWidth; x++)
                {
                    float srcX = x * scaleX;
                    float srcY = y * scaleY;
                    int x0 = (int)srcX;
                    int y0 = (int)srcY;
                    int x1 = Math.Min(x0 + 1, width - 1);
                    int y1 = Math.Min(y0 + 1, height - 1);

                    float fx = srcX - x0;
                    float fy = srcY - y0;

                    float a = input[y0 * width + x0];
                    float b = input[y0 * width + x1];
                    float c = input[y1 * width + x0];
                    float d = input[y1 * width + x1];

                    float value = a * (1 - fx) * (1 - fy) +
                                  b * fx * (1 - fy) +
                                  c * (1 - fx) * fy +
                                  d * fx * fy;

                    output[y * newWidth + x] = value;
                }
            });

            return output;
        }
       
        private static float[] ApplyCircularBlur(float[] input, int width, int height, int kernelSize)
        {
            float[] output = new float[input.Length];
            int radius = kernelSize / 2;
            int[] xOffsets = new int[kernelSize * kernelSize];
            int[] yOffsets = new int[kernelSize * kernelSize];
            int kernelCount = 0;

            // Pre-compute kernel offsets
            for (int ky = -radius; ky <= radius; ky++)
            {
                for (int kx = -radius; kx <= radius; kx++)
                {
                    if (kx * kx + ky * ky <= radius * radius)
                    {
                        xOffsets[kernelCount] = kx;
                        yOffsets[kernelCount] = ky;
                        kernelCount++;
                    }
                }
            }

            // Apply blur
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0;
                    int count = 0;

                    for (int k = 0; k < kernelCount; k++)
                    {
                        int sampleX = x + xOffsets[k];
                        int sampleY = y + yOffsets[k];

                        if (sampleX >= 0 && sampleX < width && sampleY >= 0 && sampleY < height)
                        {
                            sum += input[sampleY * width + sampleX];
                            count++;
                        }
                    }

                    output[y * width + x] = count > 0 ? sum / count : 0;
                }
            });

            return output;
        }
    }
}
