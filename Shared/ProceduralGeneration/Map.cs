using System;
using System.Drawing;
using System.Collections.Generic;
using Shared.ProceduralGeneration.Island;
using Shared.ProceduralGeneration.Util;
using static Shared.ProceduralGeneration.Island.GenerateBiomes;
using System.Threading;

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
            Random random = new Random(seed);
            for (int i = 0; i < x; i++) {
                for (int j = 0; j < y; j++) { 
                    Color randomGreen = Color.FromArgb(0, 100, 0);
                    int textureIndex = AddOrGetTextureType(new Texture("Void", randomGreen));
                    int tileIndex = AddOrGetTileType(new Tile(textureIndex));
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

        public void Generate(int seed, Action<string> Debug) {
            Debug("start");

            GenerateEmpty(seed);
            Debug("empty");

            GenerateBiomes.SetupDict();

            float[,] islandHeightMap = GenerateShape.GenerateIsland(this, seed);
            Debug("GenerateIsland");

            bool[,] landMask = MaskUtils.GetHigherThan(islandHeightMap, 0.1f);
            Debug("GetHigherThan");

            bool[,] waterMask = MaskUtils.CreateReverseMask(landMask);
            Debug("CreateReverseMask");
            
            int riverAmmount = (int) (getM2() / 1000000) * 3;
            bool[,] riverMask = GenerateRivers.Generate(this, waterMask, islandHeightMap, seed, riverAmmount);
            Debug("GenerateRivers");


            float[,] convolutedSeaMap = GetWeather.Convolution(waterMask, 200);
           Debug("Convolution");

            float[,] humidityMap = null;
            float[,] temperatureMap = null;

            Thread humidityThread = new Thread(() => {
                humidityMap = GetWeather.GetHumidity(this, convolutedSeaMap, islandHeightMap, riverMask, seed);
            });

            Thread temperatureThread = new Thread(() => {
                temperatureMap = GetWeather.GetTemperature(this, convolutedSeaMap, islandHeightMap, riverMask, seed);
            });

            humidityThread.Start();
            temperatureThread.Start();

            humidityThread.Join();
            temperatureThread.Join();

            Debug("GetHumidity and GetTemperature");

            Biome[,] biomeMap = GenerateBiomes.Generate(this, waterMask, temperatureMap, humidityMap, islandHeightMap, convolutedSeaMap);
            Debug("GenerateBiomes");
            for (int i = 0; i < x; i++) {
                for (int j = 0; j < y; j++) {
                    Biome biome = biomeMap[i, j];
                    Paint(new Texture(biome.ToString(), GenerateBiomes.BiomeConfigurations[biome].Color), i, j);
                }
            }
            Debug("PaintBiomes");


            MaskUtils.PaintMask(this, riverMask, new Texture("Water", Color.FromArgb(0, 153, 255)), null);
            Debug("PaintRivers");
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

