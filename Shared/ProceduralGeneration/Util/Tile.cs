namespace Shared.ProceduralGeneration.Util
{
    public class Tile
    {
        public int TextureIndex;
        public Tile(int textureIndex) {
            TextureIndex = textureIndex;
        }

        public string Text { get; set; } = "";
    }
}