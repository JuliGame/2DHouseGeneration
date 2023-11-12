namespace HouseGeneration.Logic.Generator.@class;


public enum RoomType {
    Hallway,
    LivingRoom,
    Corridor,
    Stairs,
    Kitchen,
    Bathroom,
    Bedroom,
    Garage,
    Other
}

public struct AbstractRoom {
    public int Area;
    public int AreaDifferenceAllowed;
    
    public float Squareness;
    public float SquarenessDifferenceAllowed;
    
    public bool HasToBeOnEdge;
    
    public RoomType RoomType = RoomType.Other;
    
    public RoomType[] AvoidRoomTypes;
    public RoomType[] MergeWith;

    public AbstractRoom(RoomType roomType, int area, float squareness, bool hasToBeOnEdge) {
        RoomType = roomType;
        Area = area;
        AreaDifferenceAllowed = (int)(area * 0.2f);
        Squareness = squareness;
        SquarenessDifferenceAllowed = squareness * 0.2f;
        HasToBeOnEdge = hasToBeOnEdge;
        AvoidRoomTypes = new RoomType[] { };
        MergeWith = new RoomType[] { };
    }
    
    public AbstractRoom(RoomType roomType, int area, float squareness, bool hasToBeOnEdge, RoomType[] avoidRoomTypes, RoomType[] mergeWith) {
        RoomType = roomType;
        Area = area;
        AreaDifferenceAllowed = (int)(area * 0.2f);
        Squareness = squareness;
        SquarenessDifferenceAllowed = squareness * 0.2f;
        HasToBeOnEdge = hasToBeOnEdge;
        AvoidRoomTypes = avoidRoomTypes;
        MergeWith = mergeWith;
    }
    
    public AbstractRoom(RoomType roomType, int area, float squareness, bool hasToBeOnEdge, int areaDifferenceAllowed, float squarenessDifferenceAllowed, RoomType[] avoidRoomTypes, RoomType[] mergeWith) {
        RoomType = roomType;
        Area = area;
        AreaDifferenceAllowed = areaDifferenceAllowed;
        Squareness = squareness;
        SquarenessDifferenceAllowed = squarenessDifferenceAllowed;
        HasToBeOnEdge = hasToBeOnEdge;
        AvoidRoomTypes = avoidRoomTypes;
        MergeWith = mergeWith;
    }
}