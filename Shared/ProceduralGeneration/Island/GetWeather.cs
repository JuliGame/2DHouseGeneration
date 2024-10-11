using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Shared.ProceduralGeneration.Util;

namespace Shared.ProceduralGeneration.Island
{
    public static class GetWeather
    {
        private static Random random = null;
        public static float[,] GetHumidity(Map map, bool[,] waterMask, float[,] heightMap, bool[,] riverMask, int seed) {
            bool[,] watersMask = MaskUtils.AddMasks(waterMask, riverMask);


            float[,] humidityMap = Convolution(watersMask, 50);
            float[,] height = MaskUtils.Multiply(heightMap, .3f);
            humidityMap = MaskUtils.AddMasks(humidityMap, height);
            // humidityMap = height;

            return humidityMap;
        }

        public static float[,] GetTemperature(Map map, bool[,] waterMask, float[,] heightMap, bool[,] riverMask, int seed) {
            bool[,] watersMask = MaskUtils.AddMasks(waterMask, riverMask);


            float[,] humidityMap = Convolution(watersMask, 50);
            float[,] height = MaskUtils.CreateReverseMask(heightMap);
            height = MaskUtils.Multiply(height, .1f);
            humidityMap = MaskUtils.AddMasks(humidityMap, height);
            // humidityMap = height;

            return height;
        }

        public static float[,] Convolution(bool[,] watersMask, int size)
        {
            int width = watersMask.GetLength(0);
            int height = watersMask.GetLength(1);

            float[,] convolution = new float[width, height];
            int[,] summedAreaTable = new int[width + 1, height + 1];

            // Calculate summed area table
            for (int x = 1; x <= width; x++)
            {
                for (int y = 1; y <= height; y++)
                {
                    summedAreaTable[x, y] = (watersMask[x - 1, y - 1] ? 1 : 0) +
                                            summedAreaTable[x - 1, y] +
                                            summedAreaTable[x, y - 1] -
                                            summedAreaTable[x - 1, y - 1];
                }
            }

            // Calculate convolution using summed area table
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int x1 = Math.Max(0, x - size);
                    int y1 = Math.Max(0, y - size);
                    int x2 = Math.Min(width - 1, x + size);
                    int y2 = Math.Min(height - 1, y + size);

                    int sum = summedAreaTable[x2 + 1, y2 + 1] -
                              summedAreaTable[x1, y2 + 1] -
                              summedAreaTable[x2 + 1, y1] +
                              summedAreaTable[x1, y1];

                    int area = (x2 - x1 + 1) * (y2 - y1 + 1);
                    convolution[x, y] = (float)sum / area;
                }
            }

            return convolution;
        }
    }
}