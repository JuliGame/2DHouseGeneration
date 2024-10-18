using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Shared.ProceduralGeneration.Util;


namespace Shared.ProceduralGeneration.Island.Cities
{
    public class CityGen
    {

        private Random random;
        public CityGen(int seed)
        {
            random = new Random(seed);
        }

        public class CityNode {
            public Vector2 Position;
            public int Radius;
        }
        public class City {
            public List<CityNode> Position;
            public bool IsCapital;
            public Color Color;
            public List<Vector2> Points;
            public List<Vector2> Edges;
            public List<Squares.Square> Squares;
            public List<Vector2> PossibleEntryPoints;
            public bool[,] BoolPoints;
            public List<Squares.House> Houses = new List<Squares.House>();
            
        }
        public List<City> GenerateCities(Map map, bool[,] landMask) {
            List<City> cities = new List<City>();
            // Capitals
            for (int i = 0; i < 2; i++) {
                Vector2 point = GetPointOnShore(landMask);
                cities.Add(new City { Position = new List<CityNode> { new CityNode { Position = point, Radius = 125 } }, IsCapital = true });
            }

            // small cities
            for (int i = 0; i < 3; i++) {
                Vector2 point = i % 2 == 0 ? GetPointOnShore(landMask) : GetPointOnLand(landMask);
                cities.Add(new City { Position = new List<CityNode> { new CityNode { Position = point, Radius = 50 } }, IsCapital = false });
            }

            // small cities
            for (int i = 0; i < 5; i++) {
                Vector2 point = i % 2 == 0 ? GetPointOnShore(landMask) : GetPointOnLand(landMask);
                cities.Add(new City { Position = new List<CityNode> { new CityNode { Position = point, Radius = 25 } }, IsCapital = false });
            }

            // Optimize city placement
            OptimizeCityPlacement(cities);

            foreach (var city in cities) {
                Color randomColor = Color.FromArgb(random.Next(255), random.Next(255), random.Next(255));
                city.Color = randomColor;
                // city.Color = GenerateBiomes.fromHex("#88357d");
                // foreach (var position in city.Position) {
                //     FillCircle(landMask, (int)position.Position.X, (int)position.Position.Y, position.Radius, map, randomColor);
                // }
            }

            return cities;
        }

        private void OptimizeCityPlacement(List<City> cities)
        {
            cities.Sort((a, b) => b.IsCapital.CompareTo(a.IsCapital)); // Prioritize capitals

            for (int i = 0; i < cities.Count; i++)
            {
                City firstCity = cities[i];
                int firstRadius = firstCity.Position[0].Radius;
                for (int j = cities.Count - 1; j > i; j--)
                {
                    City secondCity = cities[j];
                    int secondRadius = secondCity.Position[0].Radius;
                    if (Vector2.DistanceSquared(firstCity.Position[0].Position, secondCity.Position[0].Position) < (firstRadius + secondRadius) * (firstRadius + secondRadius))
                    {
                        cities.RemoveAt(j);
                        firstCity.Position.Add(secondCity.Position[0]);
                    }
                }
            }
        }

        private static void FillCircle(bool[,] mask, int centerX, int centerY, int radius, Map map, Color color)
        {
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);

            for (int x = Math.Max(0, centerX - radius); x < Math.Min(width, centerX + radius + 1); x++)
            {
                for (int y = Math.Max(0, centerY - radius); y < Math.Min(height, centerY + radius + 1); y++)
                {
                    if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) <= radius * radius)
                    {
                        if (mask[x, y]) {
                            map.Paint(new Texture("City", color), x, y);
                        }
                    }
                }
            }
        }

        Vector2 GetPointOnLand(bool[,] landMask) {
            int width = landMask.GetLength(0);
            int height = landMask.GetLength(1);

            Vector2 point = new Vector2(random.Next(width), random.Next(height));
            while (!landMask[(int)point.X, (int)point.Y]) {
                point = new Vector2(random.Next(width), random.Next(height));
            }

            return point;
        }

        Vector2 GetPointOnShore(bool[,] landMask) {
            Vector2 point = GetPointOnLand(landMask);

            int angle = random.Next(0, 360) * 10;
            Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            while (landMask[(int) point.X, (int) point.Y]) {
                point += direction;
            }
            point -= direction;

            return point;
        }
    }
}
