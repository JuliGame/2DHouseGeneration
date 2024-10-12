namespace Shared.ProceduralGeneration.Util
{
    public class Wall
    {
        public int TextureIndex;
        public bool isHalf;
        public bool isTopOrLeft;
        public float Thickness;

        public Wall(int textureIndex) {
            TextureIndex = textureIndex;
            isHalf = false;
            isTopOrLeft = false;
            Thickness = .3f;
        }
    }
}