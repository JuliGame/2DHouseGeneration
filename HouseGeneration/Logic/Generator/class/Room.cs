using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using Microsoft.Xna.Framework;

namespace HouseGeneration.Logic;

public class Room
{
    static Random random = new Random();
    public List<(int, int)> points = new List<(int, int)>();

    public int Width
    {
        get { return points.Max(p => p.Item1) - points.Min(p => p.Item1) + 1; }
    }

    public int Height
    {
        get { return points.Max(p => p.Item2) - points.Min(p => p.Item2) + 1; }
    }

    public bool MustBeAtEdge { get; set; }
    public string Text { get; set; }

    public Color Color;

    public Room(int width, int height, int x, int y, bool mustBeAtEdge = false, Color color = default)
    {
        for (int x1 = 0; x1 < width; x1++)
        {
            for (int y1 = 0; y1 < height; y1++)
            {
                points.Add((x + x1, y + y1));
            }
        }

        MustBeAtEdge = mustBeAtEdge;

        if (color != default)
        {
            Color = color;
        }
        else
        {
            Color = Color.FromNonPremultiplied(random.Next(255), random.Next(255), random.Next(255), 255);
        }
    }

    public Room(int area, float noSquareness, bool mustBeAtEdge = false, Color color = default)
    {
        if (noSquareness <= 0)
            noSquareness = 0.01f;
        else if (noSquareness >= 1)
            noSquareness = 0.99f;

        int Width, Height;
        if (noSquareness == 1)
        {
            // La mayor erea cuadrada posible
            Width = Height = (int)Math.Sqrt(area);
        }
        else if (noSquareness == 0)
        {
            // Un rectangulo de 1 de altura
            Width = area;
            Height = 1;
        }
        else
        {
            // Un rectangulo de una altura proporcionalmente menor a la de un cuadrado perfecto
            Height = (int)Math.Sqrt(area) - (int)(Math.Sqrt(area) * noSquareness);
            if (Height == 0)
            {
                Height = 1;
            }

            Width = area / Height;
        }

        if (random.NextDouble() < 0.5)
        {
            (Width, Height) = (Height, Width);
        }

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                points.Add((x, y));
            }
        }


        MustBeAtEdge = mustBeAtEdge;

        if (color != default)
        {
            Color = color;
        }
        else
        {
            Color = Color.FromNonPremultiplied(random.Next(255), random.Next(255), random.Next(255), 255);
        }
    }

    public Room(List<(int, int)> points, Color color = default)
    {
        this.points = points;

        if (color != default)
        {
            Color = color;
        }
        else
        {
            Color = Color.FromNonPremultiplied(random.Next(255), random.Next(255), random.Next(255), 255);
        }
    }

    public List<(int, int)> GetExterior()
    {
        List<(int, int)> exterior = new List<(int, int)>();

        for (int x = points.Min(p => p.Item1) - 1; x <= points.Max(p => p.Item1) + 1; x++)
        {
            for (int y = points.Min(p => p.Item2) - 1; y <= points.Max(p => p.Item2) + 1; y++)
            {
                if (points.Contains((x, y)))
                {
                    continue;
                }

                exterior.Add((x, y));
            }
        }

        return exterior;
    }

    public List<(int, int)> GetPoinsOfSide(Side side)
    {
        List<(int, int)> finalPoints = new List<(int, int)>();

        switch (side)
        {
            case Side.Right:
                for (int y = points.Min(p => p.Item2); y <= points.Max(p => p.Item2); y++)
                {
                    finalPoints.Add((points.Max(p => p.Item1), y));
                }

                break;

            case Side.Left:
                for (int y = points.Min(p => p.Item2); y <= points.Max(p => p.Item2); y++)
                {
                    finalPoints.Add((points.Min(p => p.Item1), y));
                }

                break;

            case Side.Top:
                for (int x = points.Min(p => p.Item1); x <= points.Max(p => p.Item1); x++)
                {
                    finalPoints.Add((x, points.Min(p => p.Item2)));
                }

                break;

            case Side.Bottom:
                for (int x = points.Min(p => p.Item1); x <= points.Max(p => p.Item1); x++)
                {
                    finalPoints.Add((x, points.Max(p => p.Item2)));
                }

                break;
        }

        return finalPoints;
        // return points;
    }

    public void Extend(Side facingSide)
    {
        switch (facingSide)
        {
            case Side.Top:
                GetPoinsOfSide(Side.Top).ForEach(p => points.Add((p.Item1, p.Item2 - 1)));
                break;
            case Side.Bottom:
                GetPoinsOfSide(Side.Bottom).ForEach(p => points.Add((p.Item1, p.Item2 + 1)));
                break;
            case Side.Left:
                GetPoinsOfSide(Side.Left).ForEach(p => points.Add((p.Item1 - 1, p.Item2)));
                break;
            case Side.Right:
                GetPoinsOfSide(Side.Right).ForEach(p => points.Add((p.Item1 + 1, p.Item2)));
                break;
        }
    }

    public void UnExtend(Side facingSide)
    {
        switch (facingSide)
        {
            case Side.Top:
                GetPoinsOfSide(Side.Top).ForEach(p => points.Remove((p.Item1, p.Item2)));
                break;
            case Side.Bottom:
                GetPoinsOfSide(Side.Bottom).ForEach(p => points.Remove((p.Item1, p.Item2)));
                break;
            case Side.Right:
                GetPoinsOfSide(Side.Right).ForEach(p => points.Remove((p.Item1, p.Item2)));
                break;
            case Side.Left:
                GetPoinsOfSide(Side.Left).ForEach(p => points.Remove((p.Item1, p.Item2)));
                break;
        }
    }

    public (float, float) GetCenter()
    {
        return (points.Min(p => p.Item1) + Width / 2f, points.Min(p => p.Item2) + Height / 2f);
    }

    public (int, int) GetTopLeft()
    {
        return (points.Min(p => p.Item1), points.Min(p => p.Item2));
    }

    public bool isNextTo(int x, int y)
    {
        // Todo mejorar esta funci?n.
        // if (x == X + Width && y >= Y && y < Y + Height) {
        //     return true;
        // }
        // else if (x == X - 1 && y >= Y && y < Y + Height) {
        //     return true;
        // }
        // else if (y == Y + Height && x >= X && x < X + Width) {
        //     return true;
        // }
        // else if (y == Y - 1 && x >= X && x < X + Width) {
        //     return true;
        // }

        return false;
    }
    
    public void MoveTo(int x, int y)
    {
        // Move the room to the given position
        // lefter points.x = x
        // upper points.y = y

        int minX = points.Min(p => p.Item1);
        int minY = points.Min(p => p.Item2);

        foreach (var (dx, dy) in points.ToArray())
        {
            points.Remove((dx, dy));
            points.Add((dx - minX + x, dy - minY + y));
        }
    }
    
    public List<Room> Divide(int size)
    {
        // List<Room> rooms = new List<Room>();
        // int remaining = size - 2;
        //
        // List<Room> dividedRooms = DivideExactlyIn(2);
        // Room room1 = dividedRooms[0];
        // Room room2 = dividedRooms[1];
        //
        // rooms.Add(room1);
        // if (remaining == 0) 
        //     rooms.Add(room2);
        //
        // rooms.AddRange(room2.Divide(remaining));
        // return rooms;

        return DivideExactlyIn(2, points.ToList());
    }


public List<Room> DivideExactlyIn(int slices, List<(int, int)> areaPoints)
    {
        List<Room> rooms = new List<Room>();
        DivideArea(rooms, areaPoints, slices);
        return rooms;
    }

    private void DivideArea(List<Room> rooms, List<(int, int)> areaPoints, int slices)
    {
        if (slices == 0 || areaPoints.Count < 3)
        {
            return;
        }

        if (slices == 1)
        {
            rooms.Add(new Room(areaPoints));
            return;
        }

        // nos fijaos en la forma de la habitaci?n
        bool divideHorizontally = Width > Height;

        if (divideHorizontally)
        {
            int splitY = RandomBetween(areaPoints, 0.5f);
            List<(int, int)> upperRoomPoints = new List<(int, int)>();
            List<(int, int)> lowerRoomPoints = new List<(int, int)>();

            foreach (var point in areaPoints)
            {
                if (point.Item2 < splitY)
                {
                    upperRoomPoints.Add(point);
                }
                else
                {
                    lowerRoomPoints.Add(point);
                }
            }

            DivideArea(rooms, upperRoomPoints, slices - 1);
            DivideArea(rooms, lowerRoomPoints, slices - 1);
        }
        else
        {
            int splitX = RandomBetween(areaPoints, 0.5f);
            List<(int, int)> leftRoomPoints = new List<(int, int)>();
            List<(int, int)> rightRoomPoints = new List<(int, int)>();

            foreach (var point in areaPoints)
            {
                if (point.Item1 < splitX)
                {
                    leftRoomPoints.Add(point);
                }
                else
                {
                    rightRoomPoints.Add(point);
                }
            }

            DivideArea(rooms, leftRoomPoints, slices - 1);
            DivideArea(rooms, rightRoomPoints, slices - 1);
        }
    }

    private int RandomBetween(List<(int, int)> points, float normalDistribution) {
        if (normalDistribution == 0)
        {
            return RandomBetween(points);
        }

        int index = (int)Math.Round(random.Next(points.Count) * 100 + points.Count / 2f);
        if (index < 0)
        {
            index = 0;
        }
        else if (index >= points.Count)
        {
            index = points.Count - 1;
        }
        return points[index].Item1;
    }

    private int RandomBetween(List<(int, int)> points)
    {
        int index = random.Next(points.Count);
        return points[index].Item1;
    }

    
    private List<(int, int)> RemovePointsFromList(List<(int, int)> sourceList, List<(int, int)> pointsToRemove)
    {
        List<(int, int)> result = new List<(int, int)>(sourceList);
        foreach (var point in pointsToRemove)
        {
            result.Remove(point);
        }
        return result;
    }
    
    
    
    // public List<Room> Divide(int size) {
    //     // divide the room in the given number of rooms
    //     // the rooms are divided in the direction of the longest side
    //     
    //     int[,] mask = new int[points.Max(p => p.Item1) - points.Min(p => p.Item1) + 1, points.Max(p => p.Item2) - points.Min(p => p.Item2) + 1];
    //     foreach (var (x, y) in points) {
    //         mask[x - points.Min(p => p.Item1), y - points.Min(p => p.Item2)] = 1;
    //     }
    //     
    //     int[,] input = Noise.CreateUniqueMatrix(size, Height, Width);
    //         
    //     Console.WriteLine("mask Matrix:");
    //     Noise.PrintMatrix(mask);
    //     
    //     Console.WriteLine("Input Matrix:");
    //     Noise.PrintMatrix(input);
    //     
    //     int numIterations = 1;
    //     int[,] output2 = Noise.ApplyCellularNoise(input, numIterations);
    //     int[,] output = input;
    //     for (int x = 0; x < Width; x++) {
    //         for (int y = 0; y < Height; y++) {
    //             if (input[x, y] == -1)
    //                 output[x, y] = -1;
    //             
    //             if (mask[x, y] == 0) {
    //                 output[x, y] = -1;
    //                 continue;
    //             }
    //             
    //             output[x, y] = output2[x, y];
    //         }
    //     }
    //     Console.WriteLine("Output Matrix:");
    //     Noise.PrintMatrix(output);
    //     
    //     int max = output.Cast<int>().Max();
    //     List<Room> rooms = new List<Room>();
    //     for (int i = 0; i < max; i++) {
    //         List<(int, int)> points = new List<(int, int)>();
    //         for (int x = 0; x < Width; x++) {
    //             for (int y = 0; y < Height; y++) {
    //                 if (output[x, y] == i) {
    //                     points.Add((x + GetTopLeft().Item1, y + GetTopLeft().Item2));
    //                 }
    //             }
    //         }
    //         rooms.Add(new Room(points));
    //     }
    //
    //     return rooms;
    // }

    private List<(int, int)> GetCorners() {
        var corners = new List<(int, int)>();
        
        for (int i = points.Min(p => p.Item1); i <= points.Max(p => p.Item1); i++) {
            for (int j = points.Min(p => p.Item2); j <= points.Max(p => p.Item2); j++) {
                if (IsCorner(i, j)) {
                    corners.Add((i, j));
                }
            }
        }
    
        return corners;
    }
    
    private bool IsCorner(int i, int j) {
        // Condiciones de límites
        
        bool isAboveFilled = points.Contains((i, j + 1));
        bool isBelowFilled = points.Contains((i, j -1));
        bool isLeftFilled = points.Contains((i - 1, j));
        bool isRightFilled = points.Contains((i + 1, j));

        // Verifica si el tile actual es una esquina en función de sus vecinos.
        // Esto es si tiene 2 vecinos juntos (L) y 2 vecinos opuestos Vacios
        // Ejemplo:
        // 0 0 0 0 0
        // 0 2 1 2 0
        // 0 1 1 1 0
        // 0 2 1 2 0
        // 0 0 0 0 0
        // los 2 son esquinas porque tiene dos 1 y dos 0.
        return points.Contains((i, j)) && 
               ((isAboveFilled && isLeftFilled && !isBelowFilled && !isRightFilled) ||
                (isAboveFilled && isRightFilled && !isBelowFilled && !isLeftFilled) ||
                (isBelowFilled && isLeftFilled && !isAboveFilled && !isRightFilled) ||
                (isBelowFilled && isRightFilled && !isAboveFilled && !isLeftFilled) ||
                (isBelowFilled && !isRightFilled && !isAboveFilled && !isLeftFilled) ||
                (!isBelowFilled && isRightFilled && !isAboveFilled && !isLeftFilled) ||
                (!isBelowFilled && !isRightFilled && isAboveFilled && !isLeftFilled) ||
                (!isBelowFilled && !isRightFilled && !isAboveFilled && isLeftFilled));
    }
    
    public Room Grow(int area, HouseGenerator.HouseBuilder generator) {
        List<(int, int)> corners = GetCorners();
        corners.Shuffle();
        
        List<(Side, Side)> expandTo = new List<(Side, Side)>() {
            (Side.Top, Side.Left),
            (Side.Top, Side.Right),
            (Side.Bottom, Side.Left),
            (Side.Bottom, Side.Right)
        };
        expandTo.Shuffle();
        
        // iterate over the corners expanding the room until it reaches the given area or it can't expand anymore
        // if it can't expand anymore, reset and try with another corner
        foreach (var corner in corners) {
            foreach (var (side1, side2) in expandTo) {
                Room room = new Room(1, 1, corner.Item1, corner.Item2);
                while (room.points.Count < area && generator.CanPlaceRoom(room)) {
                    room.Extend(side1);
                    room.Extend(side2);
                }

                if (!generator.CanPlaceRoom(room)) {
                    room.UnExtend(side1);
                    room.UnExtend(side2);
                    
                    // Try expanding to only one side if it can't expand to both
                    while (room.points.Count < area && generator.CanPlaceRoom(room)) {
                        room.Extend(side1);
                    }
                    if (!generator.CanPlaceRoom(room)) {
                        room.UnExtend(side1);
                        
                        while (room.points.Count < area && generator.CanPlaceRoom(room)) {
                            room.Extend(side2);
                        }
                    }
                }

                if (room.points.Count == area && generator.CanPlaceRoom(room)) {
                    generator.TryToPlaceRoom(room);
                    return room;
                }
            }
        }
        return null;
    }
}