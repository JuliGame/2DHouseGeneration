namespace Shared.ProceduralGeneration.Util
{

    public enum Side
    {
        Top,
        Right,
        Bottom,
        Left
    }

    public static class SideExt
    {

        public static bool IsUpOrDown(this Side side)
        {
            return side == Side.Top || side == Side.Bottom;
        }

        public static Side GetPerpendicularSide(this Side side, bool isClockwise)
        {
            switch (side)
            {
                case Side.Top:
                    return isClockwise ? Side.Right : Side.Left;
                case Side.Right:
                    return isClockwise ? Side.Bottom : Side.Top;
                case Side.Bottom:
                    return isClockwise ? Side.Left : Side.Right;
                case Side.Left:
                    return isClockwise ? Side.Top : Side.Bottom;
                default:
                    return Side.Top;
            }
        }

        public static Side Invert(this Side side)
        {
            switch (side)
            {
                case Side.Top:
                    return Side.Bottom;
                case Side.Right:
                    return Side.Left;
                case Side.Bottom:
                    return Side.Top;
                case Side.Left:
                    return Side.Right;
                default:
                    return Side.Top;
            }
        }

        public static int GetX(this Side side)
        {
            switch (side)
            {
                case Side.Top:
                    return 0;
                case Side.Right:
                    return 1;
                case Side.Bottom:
                    return 0;
                case Side.Left:
                    return -1;
                default:
                    return 0;
            }
        }

        public static int GetY(this Side side)
        {
            switch (side)
            {
                case Side.Top:
                    return -1;
                case Side.Right:
                    return 0;
                case Side.Bottom:
                    return 1;
                case Side.Left:
                    return 0;
                default:
                    return 0;
            }
        }
    }
}