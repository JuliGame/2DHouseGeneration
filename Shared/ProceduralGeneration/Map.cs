using System;
using System.Drawing;
using System.Collections.Generic;
using Shared.ProceduralGeneration.Island;
using Shared.ProceduralGeneration.Util;
using static Shared.ProceduralGeneration.Island.GenerateBiomes;
using System.Threading.Tasks;
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
            int wallTextureIndex = AddOrGetTextureType(new Texture("Wall", Color.Black));
            for (int i = 0; i < x*2+1; i++) {
                for (int j = 0; j < y*2+1; j++) {
                    Walls[i, j] = new Wall(wallTextureIndex);
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
        
            HouseGenerator.Generate(this, seed, 0, 20, 0, 20);
        }

        public int getM2() {
            return x * y;
        }

        public void Generate(int seed, Action<string, bool> Debug, bool useCPU) {
            MapChanged = true;

            GenerateBiomes.SetupDict();

            Debug("Heightmap", false);
            float[,] islandHeightMap = GenerateShape.GenerateIsland(this, seed);
            Debug("Heightmap", true);


            bool[,] landMask = MaskUtils.GetHigherThan(islandHeightMap, 0.1f);
            bool[,] waterMask = MaskUtils.CreateReverseMask(landMask);

            Debug("GenerateRivers", false);
            int riverAmmount = Math.Min(100, (int) (getM2() / 1000000) * 3);
            bool[,] riverMask = GenerateRivers.Generate(this, waterMask, islandHeightMap, seed, riverAmmount);
            Debug("GenerateRivers", true);

            // MaskUtils.PaintMask(this, waterMask, new Texture("Water", fromHex("#004c8a")), null);

            Debug("Weather", false);
            Debug("Weather-Convolute", false);
            float[,] convolutedSea = ConvolutionUtil.Blur(waterMask, 5);
            float[,] convolutedRiver = ConvolutionUtil.Blur(riverMask, 5);
            Debug("Weather-Convolute", true);


            Debug("Weather-Humidity", false);
            float[,] humidityMap = CPUHumidity.GetHumidity(this, convolutedSea, islandHeightMap, riverMask, seed);
            Debug("Weather-Humidity", true);

            Debug("Weather-Temperature", false);
            float[,] temperatureMap = TemperatureCalculator.GetTemperature(this, seed, islandHeightMap);
            Debug("Weather-Temperature", true);
            Debug("Weather", true);

            Debug("GenerateBiomes", false);
            Biome[,] biomeMap = GenerateBiomes.Generate(this, waterMask, temperatureMap, humidityMap, islandHeightMap, convolutedSea);
            Debug("GenerateBiomes", true);

            Debug("Final Paint", false);
            for (int i = 0; i < x; i++) {
                for (int j = 0; j < y; j++) {
                    Biome biome = biomeMap[i, j];
                    Color biomColor = BiomeConfigurations[biome].Color;
                    Paint(new Texture(biome.ToString(), biomColor), i, j);
                }
            }
            MaskUtils.PaintMask(this, riverMask, new Texture("Water", fromHex("#0872c9")), null);
            Debug("Final Paint", true);
            MapChanged = true;

            CityGen cityGen = new CityGen(seed);
            List<CityGen.City> cities = cityGen.GenerateCities(this, landMask);

            VoronoiDiagram voronoiDiagram = new VoronoiDiagram(seed);
            voronoiDiagram.Generate(this, landMask);
            voronoiDiagram.PaintCities(this, cities, landMask);

            RoadNetwork roadNetwork = new RoadNetwork(seed, landMask);
            Squares squares = new Squares(seed, landMask, roadNetwork);

            squares.GenerateSquares(this, cities);

            int num = 0;
            foreach (var city in cities) {
                foreach (var house in city.Houses) {
                    num++;
                    if (house.IsOnSquare) {
                        for (int x = house.XMin; x <= house.XMax; x++) {
                            for (int y = house.YMin; y <= house.YMax; y++) {
                                Paint(new Texture("Voronoi", fromHex("#c9b1a1")), x, y);
                            }
                        }
                    }
                    try {
                        HouseGenerator.Generate(this, seed + num, house.XMin, house.XMax + 1, house.YMin, house.YMax + 1);
                    } catch (Exception e) {
                        Console.WriteLine(e);
                    }
                }
            }

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