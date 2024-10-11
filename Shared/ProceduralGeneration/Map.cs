using System;
using System.Drawing;
using Shared.ProceduralGeneration.Island;
using Shared.ProceduralGeneration.Util;
using static Shared.ProceduralGeneration.Island.GenerateBiomes;

namespace Shared.ProceduralGeneration
{
    public class Map
    {
        public readonly int x;
        public readonly int y;
        public bool MapChanged = false;

        Tile [,] tiles;
        Wall[,] walls;
        public Map(int x, int y) {
            this.x = x;
            this.y = y;
        
            tiles = new Tile[x, y];
            walls = new Wall[x*2+1, y*2+1];
        
            GenerateEmpty(0);
        }

        private void GenerateEmpty(int seed) {
            Random random = new Random(seed);
            for (int i = 0; i < x; i++) {
                for (int j = 0; j < y; j++) { 
                    int shade = random.Next(50, 150);
                    Color randomGreen = Color.FromArgb(0, shade, 0);
                    tiles[i, j] = new Tile(new Texture("Grass", randomGreen));
                }
            }
        
            for (int i = 0; i < x*2+1; i++) {
                for (int j = 0; j < y*2+1; j++) { 
                    walls[i, j] = new Wall(new Texture("Empty", Color.FromArgb(0, 0,0,0)));
                }
            }
        }

        public void GenerateHouse(int seed) {
            GenerateEmpty(seed);
        
            HouseGenerator.Generate(this, seed);
        }

        public int getM2() {
            return x * y;
        }

        public void Generate(int seed) {
            GenerateEmpty(seed);
            Console.WriteLine("Generating map");

            float[,] islandHeightMap = GenerateShape.GenerateIsland(this, seed);
            bool[,] landMask = MaskUtils.GetHigherThan(islandHeightMap, 0.1f);


            // MaskUtils.PaintMask(this, landMask, new Texture("Grass", Color.FromArgb(0, 150, 0)), new Texture("Water", Color.FromArgb(0, 0, 153)));            
            bool[,] waterMask = MaskUtils.CreateReverseMask(landMask);
            
            int riverAmmount = (int) (getM2() / 1000000) * 3;
            bool[,] riverMask = GenerateRivers.Generate(this, waterMask, islandHeightMap, seed, riverAmmount);
            // MaskUtils.PaintMask(this, landMask, null, new Texture("Water", Color.FromArgb(0, 0, 150)));

            float[,] humidityMap = GetWeather.GetHumidity(this, waterMask, islandHeightMap, riverMask, seed);
            // MaskUtils.DebugPaintFloatMask(this, humidityMap);

            
            float[,] temperatureMap = GetWeather.GetTemperature(this, waterMask, islandHeightMap, riverMask, seed);
            // MaskUtils.DebugPaintFloatMask(this, temperatureMap);

            Biome[,] biomeMap = GenerateBiomes.Generate(this, waterMask, temperatureMap, humidityMap, islandHeightMap);
            for (int i = 0; i < x; i++) {
                for (int j = 0; j < y; j++) {
                    Biome biome = biomeMap[i, j];
                    Paint(new Texture(biome.ToString(), GenerateBiomes.BiomeConfigurations[biome].Color), i, j);
                }
            }

            MaskUtils.PaintMask(this, riverMask, new Texture("Water", Color.FromArgb(0, 153, 255)), null);

            MapChanged = true;
        }


        public void Paint(Texture color, int x, int y, string text = null) {
            if (x < 0 || x >= this.x || y < 0 || y >= this.y) return;
        
            tiles[x, y].Texture = color;
            tiles[x, y].Text = text;
        }
    
        public void Paint(Texture color, int x, int y, Side side) {
            int wallX = x * 2 + 1 + side.GetX();
            int wallY = y * 2 + 1 + side.GetY();
            walls[wallX, wallY].Texture = color;
        }
        public void PaintWall(Texture color, int wallX, int wallY, bool half = false, bool topLeft = false, float thickness = .3f) {
            walls[wallX, wallY].Texture = color;
            walls[wallX, wallY].isHalf = half;
            walls[wallX, wallY].isTopOrLeft = topLeft;
            walls[wallX, wallY].Thickness = thickness;
        }
    
        public Tile GetTile(int x, int y) {
            return tiles[x, y];
        }
    
        public Wall GetWall(int x, int y) {
            return walls[x, y];
        }
    }
}