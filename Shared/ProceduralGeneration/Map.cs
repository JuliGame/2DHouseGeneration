using System;
using System.Drawing;

using Shared.ProceduralGeneration.Util;

namespace Shared.ProceduralGeneration
{
    public class Map
    {
        public readonly int x;
        public readonly int y;
    
        Tile [,] tiles;
        Wall[,] walls;
        public Map(int x, int y) {
            this.x = x;
            this.y = y;
        
            tiles = new Tile[x, y];
            walls = new Wall[x*2+1, y*2+1];
        
            GenerateEmpty();
        }

        private void GenerateEmpty() {
            for (int i = 0; i < x; i++) {
                for (int j = 0; j < y; j++) { 
                    tiles[i, j] = new Tile(new Texture("Grass", Color.Green));
                }
            }
        
            for (int i = 0; i < x*2+1; i++) {
                for (int j = 0; j < y*2+1; j++) { 
                    walls[i, j] = new Wall(new Texture("Empty", Color.FromArgb(0, 0,0,0)));
                }
            }
        }

        public void Generate(int seed) {
            GenerateEmpty();
        
            HouseGenerator.Generate(this, seed);
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