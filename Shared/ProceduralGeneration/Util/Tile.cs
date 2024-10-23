using System;

namespace Shared.ProceduralGeneration.Util
{
    [Serializable]
    public class Tile
    {
        public int TextureIndex;
        public Tile(int textureIndex) {
            TextureIndex = textureIndex;
        }

        public string Text { get; set; } = "";
    }
}