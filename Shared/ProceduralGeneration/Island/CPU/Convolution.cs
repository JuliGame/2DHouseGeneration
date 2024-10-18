using System;

namespace Shared.ProceduralGeneration.Island
{
    public static class ConvolutionUtil
    {   
        public static float[] Blur(float[] input, int width, int height, int kernelSize)
        {
            // Circular blur
            return ApplyCircularBlur(input, width, height, kernelSize);
        }

        private static float[] ApplyCircularBlur(float[] input, int width, int height, int kernelSize)
        {
            float[] output = new float[input.Length];
            int radius = kernelSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
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
            }

            return output;
        }

        public static float[] SquareBlur(float[] input, int width, int height, int kernelSize)
        {
            return ApplySquareBlur(input, width, height, kernelSize);
        }

        private static float[] ApplySquareBlur(float[] input, int width, int height, int kernelSize)
        {
            float[] output = new float[input.Length];
            int halfKernel = kernelSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
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
            }

            return output;
        }

        public static float[] GaussianBlur(float[] input, int width, int height, int kernelSize)
        {
            float[] kernel = CreateGaussianKernel(kernelSize);
            return ApplyConvolution(input, width, height, kernel, kernelSize);
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
                    if (index < 0 || index >= kernel.Length)
                    {
                        // Skip this iteration if the index is out of bounds
                        continue;
                    }
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

        private static float[] ApplyConvolution(float[] input, int width, int height, float[] kernel, int kernelSize)
        {
            float[] output = new float[input.Length];
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
                                sum += input[sy * width + sx] * kernelValue;
                                weightSum += kernelValue;
                            }
                        }
                    }
                    output[y * width + x] = weightSum > 0 ? sum / weightSum : 0;
                }
            }

            return output;
        }

        public static float[,] Blur(bool[,] input, int kernelSize)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);
            float[,] output = new float[height, width];
            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                    output[i, j] = input[i, j] ? 1f : 0f;

            float[,] output2D = Blur(output, kernelSize);
            return output2D;
        }

        public static float[,] Blur(float[,] input, int kernelSize)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);
            float[,] output = new float[height, width];
            int radius = kernelSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
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
                                    sum += input[sampleY, sampleX];
                                    count++;
                                }
                            }
                        }
                    }

                    output[y, x] = count > 0 ? sum / count : 0;
                }
            }

            return output;
        }
    }
}
