using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using ILGPU.IR.Transformations;
using Shared.ProceduralGeneration.Util;
using static Shared.ProceduralGeneration.Island.Cities.CityGen;

namespace Shared.ProceduralGeneration.Island.Cities
{
    public class Squares
    {
        private Random random;
        private bool[,] landMask;
        private int width;
        private int height;
        public class Square {
            public int XMin, XMax, YMin, YMax;
            public Color Color;
            public List<Vector2> Points;
        }
        public class House {
            public int XMin, XMax, YMin, YMax;
            public Color Color;
            public List<Vector2> Points;
            public bool IsOnSquare;
        }

        public RoadNetwork roadNetwork;

        public Squares(int seed, bool[,] landMask, RoadNetwork roadNetwork)
        {
            random = new Random(seed);
            this.landMask = landMask;
            this.width = landMask.GetLength(0);
            this.height = landMask.GetLength(1);
            this.roadNetwork = roadNetwork;
        }

        public void GenerateSquares(Map map, List<CityGen.City> cities) {

            bool[,] prohibited = new bool[width, height];
            Dictionary<City, List<Vector2>> citiesToFill = new Dictionary<City, List<Vector2>>();

            for (int i = 0; i < cities.Count; i++) {
                List<Square> squares = new List<Square>();
                CityGen.City city = cities[i];

                int cityWidth = (int)city.Points.Max(p => p.X) - (int)city.Points.Min(p => p.X);
                int cityHeight = (int)city.Points.Max(p => p.Y) - (int)city.Points.Min(p => p.Y);
                int cityMinX = (int)city.Points.Min(p => p.X);
                int cityMinY = (int)city.Points.Min(p => p.Y);


                List<int> verticalRoads = new List<int>();
                int lastVerticalRoad = 0;
                int minDistance = 20;
                for (int x = 0; x < cityWidth; x++) {
                    if (x - lastVerticalRoad - 7 >= minDistance && random.Next(0, 100) < 20) {
                        verticalRoads.Add(x);
                        lastVerticalRoad = x;
                    }
                }

                List<int> horizontalRoads = new List<int>();
                int lastHorizontalRoad = 0;
                minDistance = 10;
                for (int y = 0; y < cityHeight; y++) {
                    if (y - lastHorizontalRoad - 7 >= minDistance && random.Next(0, 100) < 20) {
                        horizontalRoads.Add(y);
                        lastHorizontalRoad = y;
                    }
                }

                // Create all the squares, they are the insides that are between a vertical and a horizontal roads and the edges of the city               
                for (int x = 0; x < verticalRoads.Count - 1; x++) {
                    for (int y = 0; y < horizontalRoads.Count - 1; y++) {
                        int x1 = verticalRoads[x] + cityMinX;
                        int x2 = verticalRoads[x + 1] + cityMinX;
                        int y1 = horizontalRoads[y] + cityMinY;
                        int y2 = horizontalRoads[y + 1] + cityMinY;

                        List<Vector2> points = new List<Vector2> {
                                new Vector2(x1, y1),
                                new Vector2(x2, y1),
                                new Vector2(x2, y2),
                                new Vector2(x1, y2)
                        };

                        bool IsThereAtleastOnePointInside = points.Any(p => city.BoolPoints[(int)p.X, (int)p.Y]);
                        bool AreAllPointsInside = points.All(p => city.BoolPoints[(int)p.X, (int)p.Y]);

                        if (AreAllPointsInside) {
                            Color color = Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
                            Square square = new Square {
                                XMin = x1,
                                XMax = x2,
                                YMin = y1,
                                YMax = y2,
                                Color = color,
                                Points = points
                            };
                        

                            squares.Add(square);
                        }
                    }
                }

                List<Vector2> PossibleEntryPoints = new List<Vector2>();
                foreach (Square square in squares) {
                    foreach (Vector2 point in square.Points) {
                        // is there this points is not surrounded by other squares then is an edge
                        bool isEdge = true;
                        foreach (Square otherSquare in squares) {
                            if (otherSquare == square)
                                continue;

                            if (otherSquare.Points.Contains(point)) {
                                isEdge = false;
                                break;
                            }
                        }
                        if (isEdge && !PossibleEntryPoints.Contains(point)) {
                            PossibleEntryPoints.Add(point);
                        }
                    }
                }

                Console.WriteLine("Possible entry points: " + PossibleEntryPoints.Count);
                city.PossibleEntryPoints = PossibleEntryPoints;

                for (int k = 0; k < PossibleEntryPoints.Count; k++) {
                    Vector2 point = PossibleEntryPoints[k];
                    // map.Paint(new Texture("Voronoi", city.Color), (int)point.X, (int)point.Y);
                }

                // continue;

                List<Vector2> nonFilledPoints = city.Points.ToList();

                // Draw the squares and roads
                foreach (Square square in squares) {
                    for (int x = square.XMin; x <= square.XMax; x++) {
                        for (int y = square.YMin; y <= square.YMax; y++) {
                            // map.Paint(new Texture("Voronoi", square.Color), x, y);
                            nonFilledPoints.Remove(new Vector2(x, y));
                            prohibited[x, y] = true;
                        }
                    }

                    int size = 7;
                    House house = new House {
                        XMin = square.XMin + size / 2,
                        XMax = square.XMax - size / 2,
                        YMin = square.YMin + size / 2,
                        YMax = square.YMax - size / 2,
                        Color = square.Color,
                        Points = square.Points,
                        IsOnSquare = true
                    };

                    city.Houses.Add(house);
                    // Draw the roads of the square
                    List<Vector2> points = new List<Vector2>();
                    for (int x = square.XMin; x <= square.XMax; x++) {
                        points.Add(new Vector2(x, square.YMin));
                        points.Add(new Vector2(x, square.YMax));
                    }
                    for (int y = square.YMin; y <= square.YMax; y++) {
                        points.Add(new Vector2(square.XMin, y));
                        points.Add(new Vector2(square.XMax, y));
                    }

                    foreach (var point in points) {
                        for (int dx = -size / 2; dx <= size / 2; dx++) {
                            for (int dy = -size / 2; dy <= size / 2; dy++) {
                                int x = (int)point.X + dx;
                                int y = (int)point.Y + dy;

                                if (x >= 0 && x < this.width && y >= 0 && y < this.height) {
                                    map.Paint(new Util.Texture("Road", Color.Gray), x, y);
                                    nonFilledPoints.Remove(new Vector2(x, y));
                                    prohibited[x, y] = true;
                                }
                            }
                        }
                    }            
                }

                for (int k = 0; k < nonFilledPoints.Count; k++) {
                    Vector2 point = nonFilledPoints[k];
                    // map.Paint(new Texture("Voronoi", Color.Green), (int)point.X, (int)point.Y);
                }

                citiesToFill.Add(city, nonFilledPoints);
            }

            roadNetwork.prohibited = prohibited;
            roadNetwork.GenerateRoads(map, cities);

            foreach (var city in cities) {
                List<Vector2> pointsToPoputale = citiesToFill[city];
                int houses = random.Next(20, 100);
                for (int k = 0; k < houses; k++) 
                {
                    Vector2 point = pointsToPoputale[random.Next(0, pointsToPoputale.Count)];

                    float multp = (houses - k) / (float) houses + 1;
                    int width = (int)(random.Next(8, 16) * multp);
                    int height = (int)(random.Next(8, 16) * multp);

                    int xMin = (int)point.X - width / 2;
                    int xMax = (int)point.X + width / 2;
                    int yMin = (int)point.Y - height / 2;
                    int yMax = (int)point.Y + height / 2;

                    List<Vector2> points = new List<Vector2> {
                        new Vector2(xMin, yMin),
                        new Vector2(xMax, yMin),
                        new Vector2(xMax, yMax),
                        new Vector2(xMin, yMax)
                    };

                    bool AreAllPointsInside = points.All(p => pointsToPoputale.Contains(p));
                    if (!AreAllPointsInside)
                        continue;       

                    Color color = Color.Brown;
                    bool isOnRoad = false;
                    for (int x = xMin - 1; x <= xMax + 1; x++) {
                        for (int y = yMin - 1; y <= yMax + 1; y++) {
                            if (roadNetwork.Roads[x, y] != null) {
                                isOnRoad = true;
                                break;
                            }
                        }
                        if (isOnRoad)
                            break;
                    }
                    if (isOnRoad)
                        continue;

                    House house = new House {
                        XMin = xMin,
                        XMax = xMax,
                        YMin = yMin,
                        YMax = yMax,
                        Color = color,
                        Points = points,
                        IsOnSquare = false
                    };

                    city.Houses.Add(house);
                    for (int x = xMin; x <= xMax; x++) {
                        for (int y = yMin; y <= yMax; y++) {
                            // map.Paint(new Texture("Voronoi", color), x, y);
                            pointsToPoputale.Remove(new Vector2(x, y));
                        }
                    }                   
                }
            }


        }
    }
}
