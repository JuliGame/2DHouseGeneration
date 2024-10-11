using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Shared.ProceduralGeneration.Util;

namespace Shared.ProceduralGeneration.Island
{
    public static class GenerateRivers
    {
        private static Random random = null;
        public static bool[,] Generate(Map map, bool[,] waterMask, float[,] mergedHeightMap, int seed, int riverCount = 5) {
            random = new Random(seed);
            bool[,] riverMask = new bool[map.x, map.y];

            for (int i = 0; i < riverCount; i++)
            {
                riverMask = MaskUtils.AddMasks(riverMask, GenerateRiver(map, waterMask, mergedHeightMap, riverMask, random));
            }

            riverMask = ExpandRiver(riverMask, 2);

            for (int i = 0; i < riverCount * 2; i++)
            {
                riverMask = MaskUtils.AddMasks(riverMask, GenerateRiver(map, waterMask, mergedHeightMap, riverMask, random));
            }

            return riverMask;
        }

        private static bool[,] ExpandRiver(bool[,] riverMask, int radius)
        {
            int width = riverMask.GetLength(0);
            int height = riverMask.GetLength(1);
            bool[,] expandedMask = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (riverMask[x, y])
                    {
                        FillCircle(expandedMask, x, y, radius);
                    }
                }
            }

            return expandedMask;
        }

        private static void FillCircle(bool[,] mask, int centerX, int centerY, int radius)
        {
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);

            for (int x = Math.Max(0, centerX - radius); x < Math.Min(width, centerX + radius + 1); x++)
            {
                for (int y = Math.Max(0, centerY - radius); y < Math.Min(height, centerY + radius + 1); y++)
                {
                    if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) <= radius * radius)
                    {
                        mask[x, y] = true;
                    }
                }
            }
        }
        
        private static bool[,] GenerateRiver(Map map, bool[,] waterMask, float[,] mergedHeightMap, bool[,] riverMask, Random random)
        {
            // we define a director vector to move the river
            (float x, float y) direction = (random.Next(-10, 10) * .1f, random.Next(-10, 10) * .1f);

            (float x, float y) startPoint = (random.Next(0, map.x), random.Next(0, map.y));

            // while point is in water generate another point
            while (waterMask[(int) startPoint.x, (int) startPoint.y]) {
                startPoint = (random.Next(0, map.x), random.Next(0, map.y));
            }

            // walk intil a water tile is found or the edge of the map is reached
            while (startPoint.x >= 0 && startPoint.x < map.x && startPoint.y >= 0 && startPoint.y < map.y && !waterMask[(int) startPoint.x, (int) startPoint.y]) {
                startPoint.x += direction.x;
                startPoint.y += direction.y;
                System.Console.WriteLine($"Walking to {startPoint.x}, {startPoint.y}");
                if (startPoint.x >= 0 && startPoint.x < map.x && startPoint.y >= 0 && startPoint.y < map.y) {
                    if (riverMask[(int)startPoint.x, (int)startPoint.y]) {
                        // If we hit an existing river, terminate
                        return new bool[map.x, map.y];
                    }
                } else {
                    break;
                }
            }

            // flip the direction vector
            direction = (-direction.x, -direction.y);

            startPoint.x += direction.x;
            startPoint.y += direction.y;

            int width = map.x;
            int height = map.y;

            bool[,] visited = new bool[width, height];
            while (startPoint.x >= 0 && startPoint.x < width && startPoint.y >= 0 && startPoint.y < height && !waterMask[(int) startPoint.x, (int) startPoint.y])
            {
                (direction.x, direction.y) = RotateVector(direction.x, direction.y, startPoint.x, startPoint.y, mergedHeightMap);

                startPoint.x += direction.x;
                startPoint.y += direction.y;     
                System.Console.WriteLine($"2 Walking to {startPoint.x}, {startPoint.y}");
                if (startPoint.x >= 0 && startPoint.x < width && startPoint.y >= 0 && startPoint.y < height) {
                    if (riverMask[(int)startPoint.x, (int)startPoint.y]) {
                        // If we hit an existing river, terminate
                        return visited;
                    }
                    visited[(int) startPoint.x, (int) startPoint.y] = true;
                    
                    // Modify the height map to create a river channel
                    ModifyHeightMap(mergedHeightMap, (int) startPoint.x, (int) startPoint.y, 10, 0.033f);
                } else {
                    break;
                }
            }
            return visited;
        }

        private static (float x, float y) RotateVector(float vx, float vy, float px, float py, float[,] mergedHeightMap) {
            // Calculate the angle of the gradient
            float gradientAngle = 0;
            if (random.Next(0, 100) < 40) {
                (float gradX, float gradY) = CalculateGradient(px, py, mergedHeightMap);
                float magnitude = (float)Math.Sqrt(gradX * gradX + gradY * gradY);
                gradientAngle = (float)Math.Atan2(gradY, gradX) * magnitude;
            }
            float randomAngle = 0;
            if (random.Next(0, 100) < 10) {
                randomAngle = (random.Next(-100, 100)) * 0.005f;
            }

            float currentAngle = (float)Math.Atan2(vy, vx);
            float finalAngle = currentAngle + gradientAngle + randomAngle;

            float length = (float)Math.Sqrt(vx * vx + vy * vy);
            return (length * (float)Math.Cos(finalAngle), length * (float)Math.Sin(finalAngle));
        }

        private static (float x, float y) CalculateGradient(float px, float py, float[,] mergedHeightMap)
        {
            int x = (int)px;
            int y = (int)py;
            int width = mergedHeightMap.GetLength(0);
            int height = mergedHeightMap.GetLength(1);

            float gradX = 0;
            float gradY = 0;

            if (x > 0 && x < width - 1)
            {
                gradX = (mergedHeightMap[x - 1, y] - mergedHeightMap[x + 1, y]) / 2;
            }
            else if (x > 0)
            {
                gradX = mergedHeightMap[x - 1, y] - mergedHeightMap[x, y];
            }
            else if (x < width - 1)
            {
                gradX = mergedHeightMap[x, y] - mergedHeightMap[x + 1, y];
            }

            if (y > 0 && y < height - 1)
            {
                gradY = (mergedHeightMap[x, y - 1] - mergedHeightMap[x, y + 1]) / 2;
            }
            else if (y > 0)
            {
                gradY = mergedHeightMap[x, y - 1] - mergedHeightMap[x, y];
            }
            else if (y < height - 1)
            {
                gradY = mergedHeightMap[x, y] - mergedHeightMap[x, y + 1];
            }

            return (gradX, gradY);
        }
        private static void ModifyHeightMap(float[,] heightMap, int x, int y, int radius, float depthFactor)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    int currentX = x + i;
                    int currentY = y + j;

                    if (currentX >= 0 && currentX < width && currentY >= 0 && currentY < height)
                    {
                        float distance = (float)Math.Sqrt(i * i + j * j);
                        if (distance <= radius)
                        {
                            float factor = 1 - (distance / radius);
                            heightMap[currentX, currentY] -= depthFactor * factor;
                            heightMap[currentX, currentY] = Math.Max(0, heightMap[currentX, currentY]); // Ensure height doesn't go below 0
                        }
                    }
                }
            }
        }
    }
}