using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using ILGPU.IR.Transformations;
using static Shared.ProceduralGeneration.Island.Cities.CityGen;

namespace Shared.ProceduralGeneration.Island.Cities
{
    public class RoadNetwork
    {
        private Random random;
        private bool[,] landMask;
        private int width;
        private int height;

        public Road[,] Roads;
        public bool[,] prohibited;
        public class Road {
            public List<Vector2> Path;
            public Color Color;
            public int RealDistance;
            public bool ConnectsCapital;
        }

        public RoadNetwork(int seed, bool[,] landMask)
        {
            random = new Random(seed);
            this.landMask = landMask;
            this.width = landMask.GetLength(0);
            this.height = landMask.GetLength(1);
        }

        public void GenerateRoads(Map map, List<CityGen.City> cities)
        {
            Roads = new Road[map.x, map.y];
            // 1. Create a graph representation
            var graph = CreateGraph();

            // 2. Connect cities with main roads
            List<Road> roads = GetConnections(map, cities, graph);

            // for (int i = 0; i < roads.Count / 2 + 1; i++) {
            //     List<Vector2> path = roads[i].Path;
            //     for (int j = 0; j < path.Count - 1; j++) {
            //         for (int j = roads.Count - 1; j >= roads.Count / 2 + 1; j--) {
                    
            //         }
            //     }                    
            // }
            foreach (var road in roads) {
                int width = road.ConnectsCapital ? 4 : 2;
                DrawRoad(map, road.Path, road.Color, width);
            }

            // 3. Add secondary roads (optional)
            AddSecondaryRoads(map, graph);

            // 4. Add local streets (optional)
            AddLocalStreets(map, graph);
        }

        private bool[,] CreateGraph()
        {
            // Create a simplified representation of the map
            // where each cell represents whether it's traversable or not
            bool[,] graph = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    graph[x, y] = landMask[x, y];
                }
            }

            return graph;
        }

        public (Vector2, Vector2) GetClosetsEntryPoints(City city, City otherCity) {
            List<Vector2> entryPoints1 = city.PossibleEntryPoints;
            List<Vector2> entryPoints2 = otherCity.PossibleEntryPoints;

            Vector2 closest1 = entryPoints1[0];
            Vector2 closest2 = entryPoints2[0];
            float minDistance = Vector2.Distance(closest1, closest2);

            foreach (var point1 in entryPoints1) {
                foreach (var point2 in entryPoints2) {
                    float distance = Vector2.Distance(point1, point2);
                    if (distance < minDistance) {
                        minDistance = distance;
                        closest1 = point1;
                        closest2 = point2;
                    }
                }
            }

            return (closest1, closest2);
        }

        private List<Road> GetConnections(Map map, List<CityGen.City> cities, bool[,] graph)
        {
            List<Road> roads = new List<Road>();

            List<(City, City)> connections = new List<(City, City)>();
            for (int i = 0; i < cities.Count - 1; i++)
            {
                for (int j = i + 1; j < cities.Count; j++)
                {
                    bool connectsCapital = cities[i].IsCapital || cities[j].IsCapital;
                    if (connectsCapital) {
                        // add to the start of the list
                        connections.Insert(0, (cities[i], cities[j]));
                    } else{
                        // add to the end of the list
                        connections.Add((cities[i], cities[j]));
                    }                    
                }
            }

            int i2 = 0;
            foreach (var c in connections) {
                // Vector2 start = c.Item1.PossibleEntryPoints[random.Next(c.Item1.PossibleEntryPoints.Count)];
                // Vector2 end = c.Item2.PossibleEntryPoints[random.Next(c.Item2.PossibleEntryPoints.Count)];
                if (c.Item1.PossibleEntryPoints.Count == 0 || c.Item2.PossibleEntryPoints.Count == 0) {
                    continue;
                }
                
                (Vector2, Vector2) entryPoints = GetClosetsEntryPoints(c.Item1, c.Item2);
                Vector2 start = entryPoints.Item1;
                Vector2 end = entryPoints.Item2;

                bool connectsCapital = c.Item1.IsCapital || c.Item2.IsCapital;
                List<Vector2> path = FindPath(graph, start, end, connectsCapital);

                Color color = connectsCapital ? GenerateBiomes.fromHex("#d6af03") : GenerateBiomes.fromHex("#808080");
                // int col = i2 * 255 / connections.Count;
                // Color color = Color.FromArgb(col, col, col);
                if (path != null){
                    roads.Add(new Road { Path = path, Color = color, RealDistance = (int)Vector2.Distance(start, end), ConnectsCapital = connectsCapital });
                    for (int i = 0; i < path.Count; i++) {
                        Roads[(int)path[i].X, (int)path[i].Y] = roads[roads.Count - 1];
                    }
                }
                i2++;
            }
            return roads;
        }

        private List<Vector2> FindPath(bool[,] graph, Vector2 start, Vector2 end, bool connectToCapital)
        {
            var openSet = new SimplePriorityQueue<Vector2>();
            Dictionary<Vector2, Vector2> cameFrom = new Dictionary<Vector2, Vector2>();
            Dictionary<Vector2, float> gScore = new Dictionary<Vector2, float>();
            Dictionary<Vector2, float> fScore = new Dictionary<Vector2, float>();

            openSet.Enqueue(start, 0);
            gScore[start] = 0;
            fScore[start] = HeuristicCostEstimate(start, end);

            while (openSet.Count > 0)
            {
                Vector2 current = openSet.Dequeue();

                if (Roads[(int)current.X, (int)current.Y] != null) {
                    Road otherRoad = Roads[(int)current.X, (int)current.Y];

                    if (!connectToCapital) {
                        if (otherRoad.ConnectsCapital) {
                            return ReconstructPath(cameFrom, current);
                        }
                        else {
                            if (Vector2.Distance(current, start) > 100) {
                                return null;
                            }
                        }
                    }
                    else {
                        return ReconstructPath(cameFrom, current);
                    }
                }

                if (prohibited[(int)current.X, (int)current.Y] && Vector2.Distance(current, start) > 10) {
                    return ReconstructPath(cameFrom, current);
                }

                if (Vector2.Distance(current, end) < 5) // Close enough to destination
                {
                    return ReconstructPath(cameFrom, current);
                }

                foreach (Vector2 neighbor in GetNeighbors(current, graph))
                {
                    float tentativeGScore = gScore[current] + 1;

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + HeuristicCostEstimate(neighbor, end);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Enqueue(neighbor, fScore[neighbor]);
                        }
                    }
                }
            }

            return null; // No path found
        }

        private float HeuristicCostEstimate(Vector2 start, Vector2 end)
        {
            return Vector2.Distance(start, end);
        }

        private List<Vector2> GetNeighbors(Vector2 current, bool[,] graph)
        {
            List<Vector2> neighbors = new List<Vector2>();
            int[] dx = { -1, 0, 1, 0, -1, -1, 1, 1 };
            int[] dy = { 0, 1, 0, -1, -1, 1, -1, 1 };

            for (int i = 0; i < 8; i++)
            {
                int newX = (int)current.X + dx[i];
                int newY = (int)current.Y + dy[i];

                if (newX >= 0 && newX < width && newY >= 0 && newY < height && graph[newX, newY])
                {
                    neighbors.Add(new Vector2(newX, newY));
                }
            }

            // Add some randomness to the neighbor order
            return neighbors.OrderBy(x => random.Next()).ToList();
        }

        private List<Vector2> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
        {
            List<Vector2> path = new List<Vector2> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();

            // Add some natural curves to the path
            return SmoothPath(path);
        }

        private Vector2 Normalize(Vector2 vector) {
            float length = (float) Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            return new Vector2(vector.X / length, vector.Y / length);
        }

        private List<Vector2> SmoothPath(List<Vector2> path)
        {
            List<Vector2> smoothedPath = new List<Vector2>();
            for (int i = 0; i < path.Count - 1; i++)
            {
                smoothedPath.Add(path[i]);
                Vector2 direction = path[i + 1] - path[i];
                float distance = Vector2.Distance(path[i], path[i + 1]);
                
                if (distance > 10)
                {
                    Vector2 midpoint = path[i] + direction * 0.5f;
                    Vector2 perpendicular = Normalize(new Vector2(-direction.Y, direction.X));
                    float offset = (float)(random.NextDouble() - 0.5) * 2; // Random offset between -1 and 1
                    Vector2 controlPoint = midpoint + perpendicular * offset;
                    
                    smoothedPath.Add(controlPoint);
                }
            }
            smoothedPath.Add(path[path.Count - 1]);
            
            return smoothedPath;
        }

        public void DrawRoad(Map map, List<Vector2> path, Color color, int size)
        {
            foreach (var point in path)
            {
                for (int dx = -size / 2; dx <= size / 2; dx++)
                {
                    for (int dy = -size / 2; dy <= size / 2; dy++)
                    {
                        int x = (int)point.X + dx;
                        int y = (int)point.Y + dy;

                        if (x >= 0 && x < this.width && y >= 0 && y < this.height)
                        {
                            map.Paint(new Util.Texture("Road", color), x, y);
                        }
                    }
                }
            }
        }

        private void AddSecondaryRoads(Map map, bool[,] graph)
        {
            // Implement secondary road generation
            // This could involve creating branches from main roads
        }

        private void AddLocalStreets(Map map, bool[,] graph)
        {
            // Implement local street generation
            // This could involve creating a grid-like pattern in city areas
        }
    }

    // Custom SimplePriorityQueue implementation
    public class SimplePriorityQueue<T>
    {
        private List<Tuple<T, float>> elements = new List<Tuple<T, float>>();

        public int Count => elements.Count;

        public void Enqueue(T item, float priority)
        {
            elements.Add(Tuple.Create(item, priority));
        }

        public T Dequeue()
        {
            int bestIndex = 0;

            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].Item2 < elements[bestIndex].Item2)
                {
                    bestIndex = i;
                }
            }

            T bestItem = elements[bestIndex].Item1;
            elements.RemoveAt(bestIndex);
            return bestItem;
        }

        public bool Contains(T item)
        {
            return elements.Any(e => e.Item1.Equals(item));
        }
    }
}
