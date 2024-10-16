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
            Mountains,
            Ice_mountains,
            Snow
        }

        public static Dictionary<Biome, BiomeConfig> BiomeConfigurations = new Dictionary<Biome, BiomeConfig> {};

        public static Color fromHex(string hex) {
            return Color.FromArgb(
                int.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                int.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                int.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber)
            );
        }
        public static void SetupDict() {
            BiomeConfigurations = new Dictionary<Biome, BiomeConfig>
            {
                { Biome.Ocean, new BiomeConfig(new Vector4(0, 0.5f, 0.5f, 1f), fromHex("#184b75")) },
                { Biome.Beach, new BiomeConfig(new Vector4(0.5f, 0.5f, 0f, .7f), fromHex("#ffe6a0")) },
                { Biome.Desert, new BiomeConfig(new Vector4(0.5f, 0.95f, 0.1f, .5f), fromHex("#dfb65f")) },
                { Biome.Grassland, new BiomeConfig(new Vector4(0.5f, 0.4f, 0.3f, .5f), fromHex("#13a700")) },
                { Biome.Forest, new BiomeConfig(new Vector4(0.5f, 0.35f, 0.7f, .5f), fromHex("#014209")) },
                { Biome.Mountains, new BiomeConfig(new Vector4(1f, 0.4f, 0.4f, .4f), fromHex("#615454")) },
                { Biome.Ice_mountains, new BiomeConfig(new Vector4(1f, 0.3f, 0.4f, .4f), fromHex("#356194")) },
                { Biome.Snow, new BiomeConfig(new Vector4(0.5f, .1f, 0.3f, .5f), fromHex("#cae0f1")) },
            };
        }

        public static Biome[,] Generate(Map map, bool[,] waterMask, float[,] temperatureMap, float[,] humidityMap, float[,] islandHeightMap, float[,] proximityToSeaMap)
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
                        float proximityToSea = proximityToSeaMap[x, y];

                        Vector4 point = new Vector4(elevation, temperature, humidity, proximityToSea);
                        biomeMap[x, y] = GetClosestBiome(point);
                    }
                }
            }

            return biomeMap;
        }

        private static Biome GetClosestBiome(Vector4 point)
        {
            Biome closestBiome = Biome.Ocean;
            float closestDistance = float.MaxValue;

            foreach (var biomeConfig in BiomeConfigurations)
            {
                float distance = Vector4.DistanceSquared(point, biomeConfig.Value.Center);
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
            public Vector4 Center { get; }
            public Color Color { get; }

            public BiomeConfig(Vector4 center, Color color)
            {
                Center = center;
                Color = color;
            }
        }

        public struct Vector4
        {
            public float X { get; }
            public float Y { get; }
            public float Z { get; }
            public float W { get; }

            public Vector4(float x, float y, float z, float w)
            {
                X = x;
                Y = y;
                Z = z;
                W = w;
            }

            public static float DistanceSquared(Vector4 a, Vector4 b)
            {
                float dx = a.X - b.X;
                float dy = a.Y - b.Y;
                float dz = a.Z - b.Z;
                float dw = a.W - b.W;
                return dx * dx + dy * dy + dz * dz + dw * dw;
            }
        }
    }
}
