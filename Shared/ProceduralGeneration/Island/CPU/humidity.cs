using System;

namespace Shared.ProceduralGeneration.Island
{
    public static class CPUHumidity
    {
        public static float[,] GetHumidity(Map map, float[,] humidityMapSea, float[,] heightMap, float[,] humidityMapRiver, int seed)
        {
            humidityMapRiver = MaskUtils.Normalize(humidityMapRiver);
            humidityMapSea = MaskUtils.Normalize(humidityMapSea);
            float[,] humidityMap = MaskUtils.AddMasks(humidityMapRiver, humidityMapSea);
            humidityMap = MaskUtils.AddMasks(humidityMapRiver, humidityMap);
            humidityMap = MaskUtils.Normalize(humidityMap);
   

   
            Console.WriteLine("Starting CPU temperature generation...");
            int width = map.x;
            int height = map.y;
            float[,] temperatureMap = new float[width, height];
            // Generate initial noise
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noise = TemperatureCalculator.FractalNoise(x, y, seed, 8, 2.0f, 0.55f, 0.0035f) * .6f;
                    temperatureMap[x, y] = noise;
                }
            }

            humidityMap = MaskUtils.AddMasks(humidityMap, temperatureMap);
            return humidityMap;
        }
    }
}
