using System;

namespace Shared.ProceduralGeneration.Island
{
    public static class CPUHumidity
    {
        public static float[,] GetHumidity(Map map, float[,] humidityMapSea, float[,] heightMap, bool[,] riverMask, int seed)
        {
            float[,] humidityMapRiver = ConvolutionUtil.Blur(riverMask, 10);
            humidityMapRiver = MaskUtils.Normalize(humidityMapRiver);
            humidityMapSea = MaskUtils.Normalize(humidityMapSea);
            float[,] humidityMap = MaskUtils.AddMasks(humidityMapRiver, humidityMapSea);
            humidityMap = MaskUtils.AddMasks(humidityMapRiver, humidityMap);
            humidityMap = MaskUtils.Normalize(humidityMap);
            float[,] height = MaskUtils.Multiply(heightMap, .7f);
            humidityMap = MaskUtils.AddMasks(humidityMap, height);
            return humidityMap;
        }
    }
}
