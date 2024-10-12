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

        public override bool Equals(object obj) {
            return obj is Texture texture && texture.Info == Info && texture.Color.R == Color.R && texture.Color.G == Color.G && texture.Color.B == Color.B;
        }
    }
}