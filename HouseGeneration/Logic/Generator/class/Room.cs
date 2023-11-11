using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace HouseGeneration.Logic.Generator.@class;

public class Room {
    public List<(int, int)> points = new List<(int, int)>();

    public int Width {
        get { return points.Max(p => p.Item1) - points.Min(p => p.Item1) + 1; }
    }

    public int Height {
        get { return points.Max(p => p.Item2) - points.Min(p => p.Item2) + 1; }
    }

    public bool MustBeAtEdge { get; set; }
    public string Text { get; set; }

    public Color Color;
    public int Id = 2;

    public Room(int width, int height, int x, int y, bool mustBeAtEdge = false, Color color = default) {
        for (int x1 = 0; x1 < width; x1++)
        {
            for (int y1 = 0; y1 < height; y1++)
            {
                points.Add((x + x1, y + y1));
            }
        }

        MustBeAtEdge = mustBeAtEdge;

        if (color != default) 
            Color = color;
        
        else 
            Color = Color.FromNonPremultiplied(HouseGenerator.random.Next(255), HouseGenerator.random.Next(255), HouseGenerator.random.Next(255), 255);
    }

    public Room(int area, float noSquareness, bool mustBeAtEdge = false, Color color = default) {
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

        if (HouseGenerator.random.NextDouble() < 0.5)
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
            Color = Color.FromNonPremultiplied(HouseGenerator.random.Next(255), HouseGenerator.random.Next(255), HouseGenerator.random.Next(255), 255);
        }
    }
    
    public float GetSquareness() {
        return Math.Min(Width, Height) / (float)Math.Max(Width, Height);
    }

    public Room(List<(int, int)> points, Color color = default) {
        this.points = points;

        if (color != default)
        {
            Color = color;
        }
        else
        {
            Color = Color.FromNonPremultiplied(HouseGenerator.random.Next(255), HouseGenerator.random.Next(255), HouseGenerator.random.Next(255), 255);
        }
    }
    

    public List<(int, int)> GetPoinsOfSide(Side side) {
        List<(int, int)> finalPoints = new List<(int, int)>();

        switch (side) {
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
    }

    public void Extend(Side facingSide) {
        switch (facingSide) {
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

    public void UnExtend(Side facingSide) {
        switch (facingSide) {
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

    public (float, float) GetCenter() {
        return (points.Min(p => p.Item1) + Width / 2f, points.Min(p => p.Item2) + Height / 2f);
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
    
    public void MoveTo(int x, int y) {
        int minX = points.Min(p => p.Item1);
        int minY = points.Min(p => p.Item2);

        foreach (var (dx, dy) in points.ToArray())
        {
            points.Remove((dx, dy));
            points.Add((dx - minX + x, dy - minY + y));
        }
    }

    
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
    
    public List<Room> Grow(int maxRooms, HouseGenerator.HouseBuilder generator, int maxSize = 1000) {
        List<(int, int)> corners = GetCorners();
        
        List<(Side, Side)> expandTo = new List<(Side, Side)>() {
            (Side.Top, Side.Left),
            (Side.Top, Side.Right),
            (Side.Bottom, Side.Left),
            (Side.Bottom, Side.Right)
        };
        // expandTo.Shuffle();
        

        List<Room> rooms = new List<Room>();
        foreach (var corner in corners) {
            Room room = new Room(1, 1, corner.Item1, corner.Item2);
            rooms.Add(room);
        }
        
        if (rooms.Count > maxRooms) {
            int diff = rooms.Count - maxRooms;
            for (int i = 0; i < diff; i++) {
                rooms.RemoveAt(HouseGenerator.random.Next(rooms.Count));
            }
        }
        
        List<(int, int)> unreachablePoints = new List<(int, int)>();
        foreach (var room in rooms.ToList()) {
            bool success = false;
            foreach (var (side1, side2) in expandTo) {
                while (generator.CanPlaceRoom(room) && !ContainsAny(unreachablePoints, room.points) && room.points.Count < maxSize) {
                    room.Extend(side1);
                    room.Extend(side2);
                }
                
                if (room.points.Count > 1 && !generator.CanPlaceRoom(room)) {
                    room.UnExtend(side1);
                    room.UnExtend(side2);
                    success = true;
                }
            }
            if (!success) {
                rooms.Remove(room);
            } else {
                unreachablePoints.AddRange(room.points);
            }
        }

        
        List<Side> expandTo2 = new List<Side>() { Side.Top, Side.Left, Side.Bottom, Side.Right };
        
        foreach (var room in rooms.ToList()) {
            bool success = false;
            unreachablePoints.RemoveAll(p => room.points.Contains(p));
            
            foreach (var side in expandTo2) {
                int initSize = room.points.Count;
                while (generator.CanPlaceRoom(room) && !ContainsAny(unreachablePoints, room.points)) {
                    room.Extend(side);
                }
                
                room.UnExtend(side);
                if (room.points.Count > initSize && !generator.CanPlaceRoom(room)) {
                    break;
                }
            }
            
            unreachablePoints.AddRange(room.points);
        }
        
        
        // foreach (var room in rooms) {
            // Console.Out.WriteLine("Width:" + room.Width);
            // Console.Out.WriteLine("Height:" + room.Height);
            // Console.Out.WriteLine("Squareness:" + room.GetSquareness());
            // generator.TryToPlaceRoom(room);
        // }
        
        return rooms;
    }
    
    public bool ContainsAny(List<(int, int)> array1, List<(int, int)> array2) {
        // check if any of the points in array1 are in array2
        return array1.Any(array2.Contains);
    }

    public List<(int, int)> GetMiddles() {
        // if even return the exact middle else return one of the two middles
        List<(int, int)> middles = new List<(int, int)>();
        middles.Add(GetPoinsOfSide(Side.Bottom)[Width / 2]);
        middles.Add(GetPoinsOfSide(Side.Top)[Width / 2]);
        middles.Add(GetPoinsOfSide(Side.Left)[Height / 2]);
        middles.Add(GetPoinsOfSide(Side.Right)[Height / 2]);
        return middles;
    }
}