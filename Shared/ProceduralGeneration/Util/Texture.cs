using System;
using System.Drawing;

namespace Shared.ProceduralGeneration.Util
{
    public class Texture {
        public String Info = "Empty";
        public Color Color;
        public int HashCode;
        
        public Texture(String info, Color color) {
            Info = info;
            Color = color;
            HashCode = Info.GetHashCode() + Color.GetHashCode();
        }
        public Texture(String info) {
            Info = info;
        }

        public override bool Equals(object obj) {
            return obj is Texture texture && texture.HashCode == HashCode;
        }
    }
}