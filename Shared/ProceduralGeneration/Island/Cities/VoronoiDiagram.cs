using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using MIConvexHull;

namespace Shared.ProceduralGeneration.Island.Cities
{
    public class VoronoiDiagram
    {
        private Random random;
        private List<double[]> points; // Add this line to store the points

        public VoronoiDiagram(int seed)
        {
            random = new Random(seed);
        }

        public void Generate(Map map, bool[,] landMask)
        {
            int width = map.x;
            int height = map.y;

            int pointsAmount = 1000;
            points = GenerateRandomPoints(width, height, pointsAmount, landMask); // Store the generated points

            var voronoi = VoronoiMesh.Create(points);

            Console.WriteLine("Amount of vertices: " + voronoi.Vertices.Count());
            Console.WriteLine("Amount of edges: " + voronoi.Edges.Count());

            // Generate random colors for each cell
            Dictionary<int, Color> cellColors = new Dictionary<int, Color>();
            for (int i = 0; i < pointsAmount; i++)
            {
                cellColors[i] = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
            }

            Console.WriteLine("Done");
        }

        public void PaintCities(Map map, List<CityGen.City> cities, bool[,] landMask) {
            foreach (var city in cities) {
                List<Vector2> paintedPoints = new List<Vector2>();
                bool[,] boolPoints = new bool[map.x, map.y];

                foreach (var position in city.Position) {
                    PaintVoronoiCellsInRadius(map, new Point((int)position.Position.X, (int)position.Position.Y), position.Radius, city.Color, landMask, paintedPoints, boolPoints);
                }
                List<Vector2> edges = GetEdges(paintedPoints, boolPoints);
                foreach (var edge in edges) {
                    map.Paint(new Util.Texture("Voronoi", city.Color), (int)edge.X, (int)edge.Y);
                }
                city.Edges = edges;
                city.Points = paintedPoints.Distinct().ToList();
                city.BoolPoints = boolPoints;
            }
        }

        public List<Vector2> GetEdges(List<Vector2> points, bool[,] boolPoints) {
            List<Vector2> edges = new List<Vector2>();

            // return poits that have no neighbors
            foreach (var point in points) {
                bool isUpperPainted = boolPoints[(int)point.X, (int)point.Y + 1];
                bool isLowerPainted = boolPoints[(int)point.X, (int)point.Y - 1];
                bool isLeftPainted = boolPoints[(int)point.X - 1, (int)point.Y];
                bool isRightPainted = boolPoints[(int)point.X + 1, (int)point.Y];

                if (!isUpperPainted || !isLowerPainted || !isLeftPainted || !isRightPainted) {
                    edges.Add(point);
                }
            }

            return edges;
        }

        private List<double[]> GenerateRandomPoints(int width, int height, int amount, bool[,] landMask)
        {
            var points = new List<double[]>();

            for (int i = 0; i < amount; i++)
            {
                int x, y;
                do
                {
                    x = random.Next(width);
                    y = random.Next(height);
                } while (!landMask[x, y]);

                points.Add(new double[] { x, y });
            }

            return points;
        }

        private int FindClosestPointIndex(int x, int y, List<double[]> points)
        {
            int closestIndex = 0;
            double minDistance = double.MaxValue;

            for (int i = 0; i < points.Count; i++)
            {
                double dx = x - points[i][0];
                double dy = y - points[i][1];
                double distance = dx * dx + dy * dy; // Using squared distance for efficiency

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        public void PaintVoronoiCellsInRadius(Map map, Point center, int radius, Color color, bool[,] landMask, List<Vector2> paintedPoints, bool[,] boolPoints)
        {
            int width = map.x;
            int height = map.y;

            // Calculate the bounding box of the circle
            int minX = Math.Max(0, center.X - radius);
            int maxX = Math.Min(width - 1, center.X + radius);
            int minY = Math.Max(0, center.Y - radius);
            int maxY = Math.Min(height - 1, center.Y + radius);

            HashSet<int> paintedCells = new HashSet<int>();

            // Iterate through the bounding box
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    // Check if the point is within the circle
                    if (IsPointInCircle(x, y, center, radius))
                    {
                        int cellIndex = FindClosestPointIndex(x, y, points);
                        if (!paintedCells.Contains(cellIndex))
                        {
                            List<Vector2> cellPaintedPoints = PaintCell(map, cellIndex, color, landMask, boolPoints);
                            paintedPoints.AddRange(cellPaintedPoints);
                            paintedCells.Add(cellIndex);
                        }
                    }
                }
            }
        }

        private bool IsPointInCircle(int x, int y, Point center, int radius)
        {
            int dx = x - center.X;
            int dy = y - center.Y;
            return dx * dx + dy * dy <= radius * radius;
        }

        private List<Vector2> PaintCell(Map map, int cellIndex, Color color, bool[,] landMask, bool[,] edges)
        {
            int width = map.x;
            int height = map.y;

            bool[,] visited = new bool[width, height];
            Queue<Point> queue = new Queue<Point>();

            // Find a starting point for the cell
            Point start = FindCellStartPoint(cellIndex, width, height);
            queue.Enqueue(start);

            List<Vector2> paintedPoints = new List<Vector2>();

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();
                int x = current.X;
                int y = current.Y;

                if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y] || !landMask[x, y])
                    continue;

                if (FindClosestPointIndex(x, y, points) != cellIndex)
                    continue;

                visited[x, y] = true;
                paintedPoints.Add(new Vector2(x, y));
                edges[x, y] = true;
                // Add neighboring pixels to the queue
                queue.Enqueue(new Point(x + 1, y));
                queue.Enqueue(new Point(x - 1, y));
                queue.Enqueue(new Point(x, y + 1));
                queue.Enqueue(new Point(x, y - 1));
            }

            return paintedPoints;
        }

        private Point FindCellStartPoint(int cellIndex, int width, int height)
        {
            // Find the center of the Voronoi cell
            double[] cellCenter = points[cellIndex];
            int centerX = (int)Math.Round(cellCenter[0]);
            int centerY = (int)Math.Round(cellCenter[1]);

            // Ensure the point is within the map boundaries
            centerX = Math.Max(0, Math.Min(centerX, width - 1));
            centerY = Math.Max(0, Math.Min(centerY, height - 1));

            return new Point(centerX, centerY);
        }
    }
}
