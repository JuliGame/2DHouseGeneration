using Microsoft.Xna.Framework;

namespace HouseGeneration.Logic;

public class Tile
{
    public Color Color;
    public Tile(Color color) {
        Color = color;
    }

    public string Text { get; set; } = "";
}