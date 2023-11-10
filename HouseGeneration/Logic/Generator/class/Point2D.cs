using System;

namespace HouseGeneration.Logic;

public class Point2D {
    public readonly int X, Y;

    public Point2D(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }
    public Point2D((int, int) xy)
    {
        this.X = xy.Item1;
        this.Y = xy.Item2;
    }
    
    public float DistanceTo(Point2D other) {
        return (float) Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
    }
    
    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType()) {
            if (obj is (int, int)) {
                (int, int) xy = ((int, int)) obj;
                return (X == xy.Item1) && (Y == xy.Item2);
            }
            
            if (obj is (float, float)) {
                (float, float) xy = ((float, float)) obj;
                return (X == xy.Item1) && (Y == xy.Item2);
            }
            return false;
        }
        
        Point2D p = (Point2D) obj;
        return (X == p.X) && (Y == p.Y);
    }

    protected bool Equals(Point2D other)
    {
        return X == other.X && Y == other.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public Point2D Extend(Side side){
        switch (side) {
            case Side.Top:
                return new Point2D(X, Y - 1);
            case Side.Bottom:
                return new Point2D(X, Y + 1);
            case Side.Left:
                return new Point2D(X - 1, Y);
            case Side.Right:
                return new Point2D(X + 1, Y);
            default:
                return null;
        }
    }

    public (int x, int y) ToTuple() {
        return (X, Y);
    }

    public Side GetSide((float, float) weAreGoingTo) {
        float xDiff = weAreGoingTo.Item1 - X;
        float yDiff = weAreGoingTo.Item2 - Y;
        if (xDiff > yDiff) {
            if (xDiff > -yDiff) {
                return Side.Right;
            }
            else {
                return Side.Top;
            }
        }
        else {
            if (xDiff > -yDiff) {
                return Side.Bottom;
            }
            else {
                return Side.Left;
            }
        }
    }
}