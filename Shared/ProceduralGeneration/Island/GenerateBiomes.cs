using System;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;
using Shared.ProceduralGeneration.Util;

namespace Shared.ProceduralGeneration.Island
{
    public static class GenerateBiomes
    {
        public enum Biome
        {
            Ocean,
            Beach,
            Grassland,
            Desert,
            Forest,
            Tundra,
            Savanna,
            Jungle,
            Mountains
        }

        public static Dictionary<Biome, BiomeConfig> BiomeConfigurations = new Dictionary<Biome, BiomeConfig>
        {
            { Biome.Ocean, new BiomeConfig(new Vector3(0, 0.5f, 0.5f), Color.FromArgb(0, 0, 150)) },
            { Biome.Beach, new BiomeConfig(new Vector3(0.05f, 0.5f, 0.5f), Color.FromArgb(255, 255, 204)) },
            { Biome.Grassland, new BiomeConfig(new Vector3(0.5f, 0.4f, 0.6f), Color.FromArgb(0, 150, 0)) },
            { Biome.Desert, new BiomeConfig(new Vector3(0.5f, 0.8f, 0.2f), Color.FromArgb(255, 255, 0)) },
            { Biome.Forest, new BiomeConfig(new Vector3(0.6f, 0.5f, 0.7f), Color.FromArgb(0, 100, 0)) },
            { Biome.Tundra, new BiomeConfig(new Vector3(0.3f, 0.2f, 0.3f), Color.FromArgb(200, 200, 200)) },
            { Biome.Savanna, new BiomeConfig(new Vector3(0.4f, 0.7f, 0.3f), Color.FromArgb(210, 180, 140)) },
            { Biome.Jungle, new BiomeConfig(new Vector3(0.7f, 0.6f, 0.9f), Color.FromArgb(0, 80, 0)) },
            { Biome.Mountains, new BiomeConfig(new Vector3(0.8f, 0.3f, 0.4f), Color.FromArgb(100, 100, 100)) },
        };

        public static Biome[,] Generate(Map map, bool[,] waterMask, float[,] temperatureMap, float[,] humidityMap, float[,] islandHeightMap)
        {
            islandHeightMap = MaskUtils.Normalize(islandHeightMap);
            temperatureMap = MaskUtils.Normalize(temperatureMap);
            humidityMap = MaskUtils.Normalize(humidityMap);

            int width = map.x;
            int height = map.y;
            Biome[,] biomeMap = new Biome[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (waterMask[x, y])
                    {
                        biomeMap[x, y] = (int)Biome.Ocean;
                    }
                    else
                    {
                        float elevation = islandHeightMap[x, y];
                        float temperature = temperatureMap[x, y];
                        float humidity = humidityMap[x, y];

                        Vector3 point = new Vector3(elevation, temperature, humidity);
                        biomeMap[x, y] = GetClosestBiome(point);
                    }
                }
            }

            return biomeMap;
        }

        private static Biome GetClosestBiome(Vector3 point)
        {
            Biome closestBiome = Biome.Ocean;
            float closestDistance = float.MaxValue;

            foreach (var biomeConfig in BiomeConfigurations)
            {
                float distance = Vector3.DistanceSquared(point, biomeConfig.Value.Center);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBiome = biomeConfig.Key;
                }
            }

            return closestBiome;
        }

        public class BiomeConfig
        {
            public Vector3 Center { get; }
            public Color Color { get; }

            public BiomeConfig(Vector3 center, Color color)
            {
                Center = center;
                Color = color;
            }
        }

        public struct Vector3
        {
            public float X { get; }
            public float Y { get; }
            public float Z { get; }

            public Vector3(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public static float DistanceSquared(Vector3 a, Vector3 b)
            {
                float dx = a.X - b.X;
                float dy = a.Y - b.Y;
                float dz = a.Z - b.Z;
                return dx * dx + dy * dy + dz * dz;
            }
        }
    }
}
