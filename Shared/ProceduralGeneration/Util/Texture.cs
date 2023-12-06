using System;
using System.Drawing;

namespace Shared.ProceduralGeneration.Util
{
    public class Texture {
        public String Info = "Empty";
        public Color Color;
        
        public Texture(String info, Color color) {
            Info = info;
            Color = color;
        }
        public Texture(String info) {
            Info = info;
        }
    }
}