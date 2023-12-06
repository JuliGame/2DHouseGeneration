using System.Drawing;

namespace Shared.ProceduralGeneration.Util
{


    public class Wall
    {
        public Texture Texture;

        public Wall(Texture texture)
        {
            Texture = texture;
        }

        public bool isHalf;
        public bool isTopOrLeft;
        public float Thickness = 1;
    }
}