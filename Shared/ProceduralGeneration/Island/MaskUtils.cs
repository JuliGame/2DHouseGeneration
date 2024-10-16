using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using ILGPU;
using ILGPU.Runtime;
using Shared.ProceduralGeneration;
using Shared.ProceduralGeneration.Util;


namespace Shared.ProceduralGeneration.Island
{
    public static class MaskUtils
    {
        public static void PaintMask(Map map, bool[,] mask, Texture trueColor, Texture falseColor)
        {
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Texture colorToPaint = mask[x, y] ? trueColor : falseColor;
                    if (colorToPaint != null)
                    {
                        map.Paint(colorToPaint, x, y);
                    }
                }
            }
        }

        public static void DebugPaintFloatMask<T>(Map map, T[,] mask) where T : IComparable<T>
        {
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);

            // Find the maximum value in the mask
            T maxValue = mask[0, 0];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (mask[x, y].CompareTo(maxValue) > 0)
                    {
                        maxValue = mask[x, y];
                    }
                }
            }

            Dictionary<int, Texture> textureCache = new Dictionary<int, Texture>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Normalize the value to [0, 1] range
                    float normalizedValue = Convert.ToSingle(mask[x, y]) / Convert.ToSingle(maxValue);
                    int grayValue = Math.Max(0, Math.Min(255, (int)(normalizedValue * 255)));
                    
                    if (!textureCache.TryGetValue(grayValue, out Texture debugColor))
                    {
                        Color color = Color.FromArgb(grayValue, grayValue, grayValue);
                        debugColor = new Texture("DebugGray", color);
                        textureCache[grayValue] = debugColor;
                    }
                    
                    map.Paint(debugColor, x, y);
                }
            }
            
            map.MapChanged = true;
        }

        public static void DebugPaintFloatMaskRGB(Map map, float[,] maskR, float[,] maskG, float[,] maskB)
        {
            int width = maskR.GetLength(0);
            int height = maskR.GetLength(1);

            // Ensure all masks have the same dimensions
            if (width != maskG.GetLength(0) || width != maskB.GetLength(0) ||
                height != maskG.GetLength(1) || height != maskB.GetLength(1))
            {
                throw new ArgumentException("All masks must have the same dimensions");
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Convert float values [0, 1] to byte values [0, 255]
                    byte r = (byte)Math.Max(0, Math.Min(255, (int)(maskR[x, y] * 255)));
                    byte g = (byte)Math.Max(0, Math.Min(255, (int)(maskG[x, y] * 255)));
                    byte b = (byte)Math.Max(0, Math.Min(255, (int)(maskB[x, y] * 255)));

                    Color color = Color.FromArgb(r, g, b);
                    Texture debugColor = new Texture("DebugRGB", color);
                    map.Paint(debugColor, x, y);
                }
            }
        }

        private static T ReverseMaskValue<T>(T value) {
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)!(bool)(object)value;
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(255 - (int)(object)value);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(1f - (float)(object)value);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(1.0 - (double)(object)value);
            }
            else
            {
                throw new ArgumentException($"Unsupported type for reverse mask: {typeof(T)}");
            }
        }

        public static T[,] CreateReverseMask<T>(T[,] originalMask)
        {

            int width = originalMask.GetLength(0);
            int height = originalMask.GetLength(1);
            T[,] reverseMask = new T[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    reverseMask[x, y] = ReverseMaskValue(originalMask[x, y]);
                }
            }

            return reverseMask;
        }
        
        public static bool[,] GetHigherThan(float[,] mask, float threshold)
        {
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);
            bool[,] result = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    result[x, y] = mask[x, y] > threshold;
                }
            }

            return result;
        }

        public static float[,] GetHigherThanFloat(float[,] mask, float threshold, bool higherThan = true)
        {
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);
            float[,] result = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (higherThan) {
                        result[x, y] = mask[x, y] > threshold ?  mask[x, y] : 0f;
                    } else {
                        result[x, y] = mask[x, y] < threshold ?  mask[x, y] : 1f;
                    }
                }
            }

            return result;
        }

        public static int[,] CreateDistanceMask(bool[,] originalMask, int maxDistance)
        {
            int width = originalMask.GetLength(0);
            int height = originalMask.GetLength(1);
            int[,] distanceMask = new int[width, height];

            // Initialize the distance mask
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    distanceMask[x, y] = originalMask[x, y] ? 0 : maxDistance + 1;
                }
            }

            // Perform two passes: top-left to bottom-right, then bottom-right to top-left
            for (int pass = 0; pass < 2; pass++)
            {
                for (int x = pass == 0 ? 0 : width - 1; pass == 0 ? x < width : x >= 0; x += pass == 0 ? 1 : -1)
                {
                    for (int y = pass == 0 ? 0 : height - 1; pass == 0 ? y < height : y >= 0; y += pass == 0 ? 1 : -1)
                    {
                        if (distanceMask[x, y] == 0) continue;

                        int minNeighbor = maxDistance + 1;

                        // Check neighboring cells (including diagonals)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;

                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    minNeighbor = Math.Min(minNeighbor, distanceMask[nx, ny]);
                                }
                            }
                        }

                        distanceMask[x, y] = Math.Min(distanceMask[x, y], minNeighbor + 1);
                    }
                }
            }

            // Cap the distances at maxDistance
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    distanceMask[x, y] = Math.Min(distanceMask[x, y], maxDistance);
                }
            }

            return distanceMask;
        }

        public static float[,] ConvertIntToFloatMask(int[,] intMask, int maxValue = 255)
        {
            int width = intMask.GetLength(0);
            int height = intMask.GetLength(1);
            float[,] floatMask = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    floatMask[x, y] = (float)intMask[x, y] / maxValue;
                }
            }

            return floatMask;
        }

        public static float[,] Normalize(float[,] floatMask)
        {
            int width = floatMask.GetLength(0);
            int height = floatMask.GetLength(1);
            float[,] normalizedMask = new float[width, height];

            float minValue = float.MaxValue;
            float maxValue = float.MinValue;

            // Find min and max values
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    minValue = Math.Min(minValue, floatMask[x, y]);
                    maxValue = Math.Max(maxValue, floatMask[x, y]);
                }
            }

            float range = maxValue - minValue;

            // Normalize values to [0, 1] range
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    normalizedMask[x, y] = (floatMask[x, y] - minValue) / range;
                }
            }

            return normalizedMask;
        }

        
        public static float[,] MultiplyFloatMasks(float[,] mask1, float[,] mask2)
        {
            int width = mask1.GetLength(0);
            int height = mask1.GetLength(1);

            if (width != mask2.GetLength(0) || height != mask2.GetLength(1))
            {
                throw new ArgumentException("Masks must have the same dimensions");
            }

            float[,] resultMask = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    resultMask[x, y] = mask1[x, y] * mask2[x, y];
                }
            }

            return resultMask;
        }

        public static float[,] MagicMerge(float[,] mask1, float[,] mask2)
        {
            int width = mask1.GetLength(0);
            int height = mask1.GetLength(1);

            if (width != mask2.GetLength(0) || height != mask2.GetLength(1))
            {
                throw new ArgumentException("Las máscaras deben tener las mismas dimensiones");
            }

            float[,] resultMask = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float value1 = mask1[x, y] * 0.5f + 0.5f;
                    float value2 = mask2[x, y];
                    resultMask[x, y] = value1 * value2 + value2 * 0.5f;
                }
            }

            return resultMask;
        }


        public static bool[,] AddMasks(bool[,] mask1, bool[,] mask2)
        {
            int width = mask1.GetLength(0);
            int height = mask1.GetLength(1);
            bool[,] resultMask = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    resultMask[x, y] = mask1[x, y] || mask2[x, y];
                }
            }

            return resultMask;
        }

        public static float[,] Add(float[,] mask1, float Number)
        {
            int width = mask1.GetLength(0);
            int height = mask1.GetLength(1);
            float[,] resultMask = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    resultMask[x, y] = mask1[x, y] + Number;
                }
            }

            return resultMask;
        }

        public static float[,] AddMasks(float[,] mask1, float[,] mask2)
        {
            int width = mask1.GetLength(0);
            int height = mask1.GetLength(1);
            float[,] resultMask = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    resultMask[x, y] = (mask1[x, y] + mask2[x, y]) * .5f;
                }
            }

            return resultMask;
        }

        public static float[,] Multiply(float[,] mask, float value)
        {
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);
            float[,] resultMask = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    resultMask[x, y] = mask[x, y] * value;
                }
            }

            return resultMask;
        }

        public static float[,] Multiply(float[,] mask, float[,] mask2)
        {
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);
            float[,] resultMask = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    resultMask[x, y] = mask[x, y] * mask2[x, y];
                }
            }

            return resultMask;
        }
    }
}