using Microsoft.Xna.Framework;

namespace HouseGeneration.Logic;

public class Map
{
    public readonly int x = 20;
    public readonly int y = 13;
    
    Tile[,] tiles;
    Wall[,] walls;
    public Map() {
        tiles = new Tile[x, y];
        walls = new Wall[x*2+1, y*2+1];
        
        Generate();
    }
    
    public void Generate() {
        for (int i = 0; i < x; i++) {
            for (int j = 0; j < y; j++) { 
                tiles[i, j] = new Tile(Color.ForestGreen);
            }
        }
        
        for (int i = 0; i < x*2+1; i++) {
            for (int j = 0; j < y*2+1; j++) { 
                walls[i, j] = new Wall(Color.Transparent);
            }
        }
        
        HouseGenerator.Generate(this);
    }


    public void Paint(Color color, int x, int y) {
        tiles[x, y].Color = color;
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
}