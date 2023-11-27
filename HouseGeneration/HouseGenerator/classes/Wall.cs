using Microsoft.Xna.Framework;

namespace HouseGeneration.Logic;

public class Wall
{
    public Color Color;
    public Wall(Color color) {
        Color = color;
    }

    public bool isHalf;
    public bool isTopOrLeft;
    public float Thickness = 1;
}