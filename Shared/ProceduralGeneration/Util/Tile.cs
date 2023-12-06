
namespace Shared.ProceduralGeneration.Util
{
    public class Tile
    {
        public Texture Texture;
        public Tile(Texture texture) {
            Texture = texture;
        }

        public string Text { get; set; } = "";
    }
}

