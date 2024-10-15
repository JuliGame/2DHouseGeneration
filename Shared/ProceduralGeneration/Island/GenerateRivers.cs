using System;
using System.Collections.Generic;
using System.Drawing;
using Shared.ProceduralGeneration.Util;
namespace Shared.ProceduralGeneration.Island
{
    public static class GenerateRivers
    {
        public static bool[,] Generate(Map map, bool[,] waterMask, float[,] mergedHeightMap, int seed, int riverCount = 5) {
            comeBack = 0;
            Random random = new Random(seed);
            bool[,] riverMask = new bool[map.x, map.y];

            for (int i = 0; i < riverCount; i++)
            {
                riverMask = MaskUtils.AddMasks(riverMask, GenerateRiver(map, waterMask, mergedHeightMap, riverMask, random));
                Console.WriteLine($"River {i} of {riverCount} (1)");
            }

            riverMask = ExpandRiver(riverMask, 2);

            for (int i = 0; i < riverCount * 2; i++)
            {
                riverMask = MaskUtils.AddMasks(riverMask, GenerateRiver(map, waterMask, mergedHeightMap, riverMask, random));
                Console.WriteLine($"River {i} of {riverCount} (2)");
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
        
        private static bool[,] GenerateRiver(Map map, bool[,] waterMask, float[,] mergedHeightMap, bool[,] riverMask, Random random, int attempt = 0)        {
            Console.WriteLine($"Starting");
            if (attempt > 10) {
                return new bool[map.x, map.y];
            }

            bool[,] riverMask_original = (bool[,]) riverMask.Clone();

            // we define a director vector to move the river
            (float x, float y) direction = (random.Next(-10, 10) * .1f, random.Next(-10, 10) * .1f);

            (float x, float y) startPoint = (random.Next(0, map.x), random.Next(0, map.y));

            // while point is in water generate another point
            while (waterMask[(int) startPoint.x, (int) startPoint.y]) {
                Console.WriteLine("1");
                startPoint = (random.Next(0, map.x), random.Next(0, map.y));
            }

            // walk intil a water tile is found or the edge of the map is reached
            while (startPoint.x >= 0 && startPoint.x < map.x && startPoint.y >= 0 && startPoint.y < map.y && !waterMask[(int) startPoint.x, (int) startPoint.y]) {
                Console.WriteLine("2");
                startPoint.x += direction.x;
                startPoint.y += direction.y;
                if (startPoint.x >= 0 && startPoint.x < map.x && startPoint.y >= 0 && startPoint.y < map.y) {
                    if (riverMask[(int)startPoint.x, (int)startPoint.y]) {
                        // If we hit an existing river, terminate
                        // return new bool[map.x, map.y];
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
            int visitedCount = 0;
            HashSet<(int x, int y, int dx, int dy)> visitedPositionsAndDirections = new HashSet<(int, int, int, int)>();

            Texture texture = new Texture("falopa", Color.Red);

            while (startPoint.x >= 0 && startPoint.x < width && startPoint.y >= 0 && startPoint.y < height && !waterMask[(int)startPoint.x, (int)startPoint.y])
            {
                Console.WriteLine("3");
                if (visitedCount > 10000)
                    break;

                int currentX = (int)startPoint.x;
                int currentY = (int)startPoint.y;

                // Check if we've been here before with the same direction
                // if (!visitedPositionsAndDirections.Add((currentX, currentY, (int) (direction.x * 360), (int) (direction.y * 360))))
                // {
                //     // If we have, break the loop
                //     Console.WriteLine("visited");
                //     break;
                // }

                (direction.x, direction.y) = RotateVector(direction.x, direction.y, startPoint.x, startPoint.y, mergedHeightMap, random);

                startPoint.x += direction.x;
                startPoint.y += direction.y;     

                if (startPoint.x >= 0 && startPoint.x < width && startPoint.y >= 0 && startPoint.y < height) 
                {
                    if (riverMask[currentX, currentY]) 
                    {
                        // If we hit an existing river, terminate
                        break;
                    }
                    visited[currentX, currentY] = true;
                    map.Paint(texture, currentX, currentY);
                    visitedCount++;
                } 
                else 
                {
                    break;
                }
            }

            // map.MapChanged = true;

            if (visitedCount < 200) {
                return GenerateRiver(map, waterMask, mergedHeightMap, riverMask_original, random, attempt + 1);
            }
            return visited;
        }

        private static float comeBack = 0;
        private static (float x, float y) RotateVector(float vx, float vy, float px, float py, float[,] mergedHeightMap, Random random) {
            // Calculate the angle of the gradient
            float currentAngle = (float)Math.Atan2(vy, vx);
            float gradientAngle = 0;
            // if (random.Next(0, 100) < 10) {
                int rays = 3;
                float apperture = 45f;
                float separation = apperture / rays;

                float highestGradient = 0;
                float highestAngle = 0;
                for (int i = 0; i < rays; i++) {
                    float angle = (i * separation) - (apperture / 2);
                    float finalAngle_temp = angle + currentAngle;
                    float gradX = (float)Math.Cos(finalAngle_temp);
                    float gradY = (float)Math.Sin(finalAngle_temp);
                    float distance = 15;
                    float gradient = mergedHeightMap[(int)(px + gradX * distance), (int)(py + gradY * distance)];
                    if (gradient > highestGradient) {
                        highestGradient = gradient;
                        highestAngle = angle;
                    }
                }

                gradientAngle = highestAngle * .0002f;
            // }

            float randomAngle = 0;
            if (comeBack != 0) {
                if (random.Next(0, 100) < 30) {
                    randomAngle = comeBack;
                    comeBack = 0;
                }
            } else if (random.Next(0, 100) < 20) {
                randomAngle = random.Next(-100, 100) * 0.005f;
                comeBack = -randomAngle;
            }

            float finalAngle = currentAngle + gradientAngle + randomAngle;

            float length = (float)Math.Sqrt(vx * vx + vy * vy);
            return (length * (float)Math.Cos(finalAngle), length * (float)Math.Sin(finalAngle));
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
