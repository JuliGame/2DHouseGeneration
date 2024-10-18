using System;
using System.Drawing;
using System.Collections.Generic;
using Shared.ProceduralGeneration.Island;
using Shared.ProceduralGeneration.Util;
using static Shared.ProceduralGeneration.Island.GenerateBiomes;
using System.Threading;
using ILGPU.Runtime;
using ILGPU;
using System.Threading.Tasks;
using System.Reflection.Metadata.Ecma335;
using Shared.ProceduralGeneration.Island.Cities;
using System.Numerics;

namespace Shared.ProceduralGeneration
{
    public class Map
    {
        public readonly int x;
        public readonly int y;
        public bool MapChanged = false;

        private int[,] tileIndices;
        public List<Tile> TileTypes;
        public List<Texture> TextureTypes;
        public Wall[,] Walls;

        public Map(int x, int y) {
            this.x = x;
            this.y = y;
        
            tileIndices = new int[x, y];
            TileTypes = new List<Tile>();
            TextureTypes = new List<Texture>();
            Walls = new Wall[x*2+1, y*2+1];

            GenerateEmpty(0);
        }

        private void GenerateEmpty(int seed) {
            Color randomGreen = Color.FromArgb(0, 100, 0);
            int textureIndex = AddOrGetTextureType(new Texture("Void", randomGreen));
            int tileIndex = AddOrGetTileType(new Tile(textureIndex));
            for (int i = 0; i < x; i++) {
                for (int j = 0; j < y; j++) { 
                    tileIndices[i, j] = tileIndex;
                }
            }
        }

        private int AddOrGetTextureType(Texture texture) {
            int index = TextureTypes.FindIndex(t => t.Equals(texture));
            if (index == -1) {
                if (TextureTypes.Count == 25500) {
                    throw new InvalidOperationException("Maximum number of texture types (255) reached.");
                }
                TextureTypes.Add(texture);
                return (TextureTypes.Count - 1);
            }
            return index;
        }

        private int AddOrGetTileType(Tile tile) {
            int index = TileTypes.FindIndex(t => t.TextureIndex == tile.TextureIndex && t.Text == tile.Text);
            if (index == -1) {
                if (TileTypes.Count == 25500) {
                    throw new InvalidOperationException("Maximum number of tile types (255) reached.");
                }
                TileTypes.Add(tile);
                return (TileTypes.Count - 1);
            }
            return index;
        }

        public void GenerateHouse(int seed) {
            GenerateEmpty(seed);
        
            HouseGenerator.Generate(this, seed);
        }

        public int getM2() {
            return x * y;
        }

        public Task<MemoryBuffer1D<float, Stride1D.Dense>> StartConvoluteThread(MemoryBuffer1D<float, Stride1D.Dense> map, int size, bool fast)
        {
            var tcs = new TaskCompletionSource<MemoryBuffer1D<float, Stride1D.Dense>>();
            
            Thread thread = new Thread(() =>
            {
                try
                {
                    Console.WriteLine("Convoluting");
                    if (fast) {
                        var result = ConvolutionUtil.SquareBlur(map, size);
                        tcs.SetResult(result);
                    } else {
                        var result = ConvolutionUtil.Blur(map, size);
                        tcs.SetResult(result);
                    }
                    Console.WriteLine("Convoluted");
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            
            thread.Start();
            return tcs.Task;
        }

        public Task<MemoryBuffer1D<float, Stride1D.Dense>> StartAllocationThread(float[,] map)
        {
            var tcs = new TaskCompletionSource<MemoryBuffer1D<float, Stride1D.Dense>>();
            
            Thread thread = new Thread(() =>
            {
                try
                {
                    var result = GPUtils.LoadTextureToGPU(GPUtils.Flatten2DArray(map));
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            
            thread.Start();
            return tcs.Task;
        }

        public Task<MemoryBuffer1D<float, Stride1D.Dense>> StartAllocationThread(bool[,] map)
        {
            var tcs = new TaskCompletionSource<MemoryBuffer1D<float, Stride1D.Dense>>();
            
            Thread thread = new Thread(() =>
            {
                try
                {
                    var result = GPUtils.LoadTextureToGPU(GPUtils.Flatten2DArray(map));
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            
            thread.Start();
            return tcs.Task;
        }

        public async void Generate(int seed, Action<string, bool> Debug, bool useCPU = false) {
            MapChanged = true;
            GenerateBiomes.SetupDict();

            Debug("Heightmap", false);
            float[,] islandHeightMap = GenerateShape.GenerateIsland(this, seed, useCPU);
            Debug("Heightmap", true);
            
            // Task<MemoryBuffer1D<float, Stride1D.Dense>> GPUHeightMap = StartAllocationThread(islandHeightMap);

            bool[,] landMask = MaskUtils.GetHigherThan(islandHeightMap, 0.1f);
            bool[,] waterMask = MaskUtils.CreateReverseMask(landMask);

            // Task<MemoryBuffer1D<float, Stride1D.Dense>> GPUWaterMask = StartAllocationThread(waterMask);

            // Debug("GenerateRivers", false);
            // int riverAmmount = Math.Min(100, (int) (getM2() / 1000000) * 3);
            // bool[,] riverMask = GenerateRivers.Generate(this, waterMask, islandHeightMap, seed, riverAmmount);
            // Debug("GenerateRivers", true);

            // MaskUtils.PaintMask(this, waterMask, new Texture("Water", fromHex("#004c8a")), null);

            
            // Task<MemoryBuffer1D<float, Stride1D.Dense>> GPURiverMask = StartAllocationThread(riverMask);

            // Debug("Weather", false);
            // Debug("Weather-WaitForAllocation", false);
            // MemoryBuffer1D<float, Stride1D.Dense> GPUWaterMask_RES = await GPUWaterMask;
            // MemoryBuffer1D<float, Stride1D.Dense> GPURiverMask_RES = await GPURiverMask;
            // Debug("Weather-WaitForAllocation", true);

            // Task<MemoryBuffer1D<float, Stride1D.Dense>> GPUconvolutedSea = StartConvoluteThread(GPUWaterMask_RES, 100, false);
            // Task<MemoryBuffer1D<float, Stride1D.Dense>> GPUconvolutedRiver = StartConvoluteThread(GPURiverMask_RES, 50, false);


            // Debug("Weather-Convolute", false);
            // MemoryBuffer1D<float, Stride1D.Dense> GPUconvolutedSea_RES = await GPUconvolutedSea;
            // MemoryBuffer1D<float, Stride1D.Dense> GPUconvolutedRiver_RES = await GPUconvolutedRiver;
            // Debug("Weather-Convolute", true);

            // Debug("Weather-Humidity", false);
            // float[,] humidityMap = HumidityCalculator.GetHumidity(this, GPUconvolutedSea_RES, await GPUHeightMap, GPUconvolutedRiver_RES, riverMask, seed, useCPU);
            // Debug("Weather-Humidity", true);

            // Debug("Weather-Temperature", false);
            // float[,] temperatureMap = TemperatureCalculator.GetTemperature(this, seed, await GPUHeightMap, useCPU); // .1 a .2 arriba en gpu
            // Debug("Weather-Temperature", true);
            // Debug("Weather", true);

            // Debug("GenerateBiomes", false);
            // Biome[,] biomeMap = GenerateBiomes.Generate(this, waterMask, temperatureMap, humidityMap, islandHeightMap, GPUtils.Unflatten1DArray(GPUconvolutedSea.Result.GetAsArray1D(), x, y));
            // Debug("GenerateBiomes", true);

            // Debug("Final Paint", false);
            // for (int i = 0; i < x; i++) {
            //     for (int j = 0; j < y; j++) {
            //         Biome biome = biomeMap[i, j];
            //         Color biomColor = BiomeConfigurations[biome].Color;
            //         // float height = Math.Max(0, Math.Min(1, (islandHeightMap[i, j] + .5f)));
            //         // Color mergedColor = Color.FromArgb((int) (biomColor.R * height), (int) (biomColor.G * height), (int) (biomColor.B * height));
            //         Paint(new Texture(biome.ToString(), biomColor), i, j);
            //     }
            // }
            // MaskUtils.PaintMask(this, riverMask, new Texture("Water", fromHex("#0872c9")), null);
            // Debug("Final Paint", true);
            // MapChanged = true;

            // GPUtils.UnloadTextureFromGPU(GPUHeightMap.Result);
            // GPUtils.UnloadTextureFromGPU(GPUWaterMask.Result);
            // GPUtils.UnloadTextureFromGPU(GPURiverMask.Result);

            MaskUtils.PaintMask(this, waterMask, new Texture("Land", fromHex("#0051ff")), null);

            CityGen cityGen = new CityGen(seed);
            List<CityGen.City> cities = cityGen.GenerateCities(this, landMask);


            VoronoiDiagram voronoiDiagram = new VoronoiDiagram(seed);
            voronoiDiagram.Generate(this, landMask);
            voronoiDiagram.PaintCities(this, cities, landMask);

            RoadNetwork roadNetwork = new RoadNetwork(seed, landMask);
            Squares squares = new Squares(seed, landMask, roadNetwork);

            squares.GenerateSquares(this, cities);
            MapChanged = true;
        }

        public void Paint(Texture texture, int x, int y, string text = null) {
            if (x < 0 || x >= this.x || y < 0 || y >= this.y) return;
        
            int textureIndex = AddOrGetTextureType(texture);
            int tileIndex = AddOrGetTileType(new Tile(textureIndex) { Text = text });
            tileIndices[x, y] = tileIndex;
        }

        public void Paint(Texture texture, int x, int y, Side side) {
            int wallX = x * 2 + 1 + side.GetX();
            int wallY = y * 2 + 1 + side.GetY();
            int textureIndex = AddOrGetTextureType(texture);
            //Walls[wallX, wallY].TextureIndex = textureIndex;
        }

        public void PaintWall(Texture texture, int wallX, int wallY, bool half = false, bool topLeft = false, float thickness = .3f) {
            int textureIndex = AddOrGetTextureType(texture);
            Walls[wallX, wallY].TextureIndex = textureIndex;
            Walls[wallX, wallY].isHalf = half;
            Walls[wallX, wallY].isTopOrLeft = topLeft;
            Walls[wallX, wallY].Thickness = thickness;
        }

        public Tile GetTile(int x, int y) {
            return TileTypes[tileIndices[x, y]];
        }

        public Wall GetWall(int x, int y) {
            return Walls[x, y];
        }
    }
}

