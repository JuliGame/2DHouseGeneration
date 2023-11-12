using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace HouseGeneration.Logic.Generator.@class;

public class WallInfo {
    public HouseRoomAssigner.RoomInfo Parent;
    public HouseRoomAssigner.RoomInfo Child;
    public bool Horizontal;
    public List<(int, int)> Points = new List<(int, int)>();
    public int Length => Points.Count;
    
    public void PaintWalls(Map map) {
        foreach (var point in Points) {
            Color color = Color.Black;
            map.PaintWall(color, point.Item1, point.Item2);
            
        }
    }
    public void MakeDoorInMiddle(Map map) {
        int middle = Length / 2;
        // paint middle wall as transparent
        if (Horizontal) {
            map.PaintWall(Color.Transparent, Points[middle].Item1, Points[middle].Item2);
            map.PaintWall(Color.Transparent, Points[middle].Item1, Points[middle].Item2 + 1);
        }
        else {
            map.PaintWall(Color.Transparent, Points[middle].Item1, Points[middle].Item2);
            map.PaintWall(Color.Transparent, Points[middle].Item1 + 1, Points[middle].Item2);
        }
    }
}