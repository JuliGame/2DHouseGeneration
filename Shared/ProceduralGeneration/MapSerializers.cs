using System.IO;
using Shared.ProceduralGeneration.Util;

namespace Shared.ProceduralGeneration
{
    public static class MapSerializers
    {
        public static byte[] ToBlob(this Map map)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                // Write map properties
                writer.Write(map.x);
                writer.Write(map.y);
                writer.Write(map.MapChanged);

                // Write tileIndices
                for (int i = 0; i < map.x; i++)
                    for (int j = 0; j < map.y; j++)
                        writer.Write(map.tileIndices[i, j]);

                // Write TileTypes
                writer.Write(map.TileTypes.Count);
                foreach (var tile in map.TileTypes)
                {
                    writer.Write(tile.TextureIndex);
                    writer.Write(tile.Text ?? "");
                }

                // Write TextureTypes
                writer.Write(map.TextureTypes.Count);
                foreach (var texture in map.TextureTypes)
                {
                    writer.Write(texture.Info);
                    writer.Write(texture.Color.ToArgb());
                }

                // Write Walls
                for (int i = 0; i < map.x * 2 + 1; i++)
                    for (int j = 0; j < map.y * 2 + 1; j++)
                    {
                        var wall = map.Walls[i, j];
                        writer.Write(wall.TextureIndex);
                        writer.Write(wall.isHalf);
                        writer.Write(wall.isTopOrLeft);
                        writer.Write(wall.Thickness);
                    }

                // Write oceanMask
                for (int i = 0; i < map.x; i++)
                    for (int j = 0; j < map.y; j++)
                        writer.Write(map.oceanMask[i, j]);

                // Write riverMask
                for (int i = 0; i < map.x; i++)
                    for (int j = 0; j < map.y; j++)
                        writer.Write(map.riverMask[i, j]);

                return ms.ToArray();
            }
        }

        public static Map FromBlob(byte[] blob)
        {
            using (MemoryStream ms = new MemoryStream(blob))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                Map map = new Map(x, y);
                map.MapChanged = reader.ReadBoolean();

                // Read tileIndices
                for (int i = 0; i < x; i++)
                    for (int j = 0; j < y; j++)
                        map.tileIndices[i, j] = reader.ReadInt32();

                // Read TileTypes
                int tileTypesCount = reader.ReadInt32();
                map.TileTypes.Clear();
                for (int i = 0; i < tileTypesCount; i++)
                {
                    int textureIndex = reader.ReadInt32();
                    string text = reader.ReadString();
                    map.TileTypes.Add(new Tile(textureIndex) { Text = text });
                }

                // Read TextureTypes
                int textureTypesCount = reader.ReadInt32();
                map.TextureTypes.Clear();
                for (int i = 0; i < textureTypesCount; i++)
                {
                    string info = reader.ReadString();
                    int argb = reader.ReadInt32();
                    map.TextureTypes.Add(new Texture(info, System.Drawing.Color.FromArgb(argb)));
                }

                // Read Walls
                for (int i = 0; i < x * 2 + 1; i++)
                    for (int j = 0; j < y * 2 + 1; j++)
                    {
                        int textureIndex = reader.ReadInt32();
                        bool isHalf = reader.ReadBoolean();
                        bool isTopOrLeft = reader.ReadBoolean();
                        float thickness = reader.ReadSingle();
                        map.Walls[i, j] = new Wall(textureIndex)
                        {
                            isHalf = isHalf,
                            isTopOrLeft = isTopOrLeft,
                            Thickness = thickness
                        };
                    }

                // Read oceanMask
                map.oceanMask = new bool[x, y];
                for (int i = 0; i < x; i++)
                    for (int j = 0; j < y; j++)
                        map.oceanMask[i, j] = reader.ReadBoolean();

                // Read riverMask
                map.riverMask = new bool[x, y];
                for (int i = 0; i < x; i++)
                    for (int j = 0; j < y; j++)
                        map.riverMask[i, j] = reader.ReadBoolean();

                return map;
            }
        }
    }
}
