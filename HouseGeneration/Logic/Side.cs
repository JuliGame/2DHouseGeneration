namespace HouseGeneration.Logic;

public enum Side
{
    Top,
    Right,
    Bottom,
    Left
}

public static class SideExt {
    
    public static bool isUpOrDown(this Side side) {
        return side == Side.Top || side == Side.Bottom;
    }
}