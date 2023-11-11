using HouseGeneration.Logic.Generator.@class;
using Microsoft.Xna.Framework;

namespace HouseGeneration.Logic;

public class Map
{
    public readonly int x;
    public readonly int y;
    
    Tile[,] tiles;
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
                tiles[i, j] = new Tile(Color.Aqua);
            }
        }
        
        for (int i = 0; i < x*2+1; i++) {
            for (int j = 0; j < y*2+1; j++) { 
                walls[i, j] = new Wall(Color.Transparent);
            }
        }
    }

    public void Generate(int seed) {
        GenerateEmpty();
        
        HouseGenerator.Generate(this, seed);
    }


    public void Paint(Color color, int x, int y, string text = null) {
        if (x < 0 || x >= this.x || y < 0 || y >= this.y) return;
        
        tiles[x, y].Color = color;
        tiles[x, y].Text = text;
    }
    
    public void Paint(Color color, int x, int y, Side side) {
        int wallX = x * 2 + 1;
        int wallY = y * 2 + 1;
        switch (side) {
            case Side.Top:
                wallY--;
                break;
            case Side.Right:
                wallX++;
                break;
            case Side.Bottom:
                wallY++;
                break;
            case Side.Left:
                wallX--;
                break;
        }
        walls[wallX, wallY].Color = color;
    }
    
    public Tile GetTile(int x, int y) {
        return tiles[x, y];
    }
    
    public Wall GetWall(int x, int y) {
        return walls[x, y];
    }

    public void Paint(Room color) {
        
    }
}