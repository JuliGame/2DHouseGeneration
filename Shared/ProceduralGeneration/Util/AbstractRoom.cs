namespace Shared.ProceduralGeneration.Util
{
    public struct AbstractRoom
    {
        public int Area;
        public int AreaMax;

        public float Squareness;
        public float SquarenessDifferenceAllowed;

        public bool HasToBeOnEdge;

        public RoomType RoomType;

        public RoomType[] AvoidRoomTypes;
        public RoomType[] MergeWith;

        public bool IsDoorSatisfied;

        public AbstractRoom(RoomType roomType, int area, float squareness, bool hasToBeOnEdge)
        {
            RoomType = roomType;
            Area = area;
            AreaMax = 10000;
            Squareness = squareness;
            SquarenessDifferenceAllowed = squareness * 0.2f;
            HasToBeOnEdge = hasToBeOnEdge;
            AvoidRoomTypes = new RoomType[] { };
            MergeWith = new RoomType[] { };
            RoomType = roomType;
            IsDoorSatisfied = false;
        }

        public AbstractRoom(RoomType roomType, int area, float squareness, bool hasToBeOnEdge,
            RoomType[] avoidRoomTypes, RoomType[] mergeWith)
        {
            RoomType = roomType;
            Area = area;
            AreaMax = 10000;
            Squareness = squareness;
            SquarenessDifferenceAllowed = squareness * 0.2f;
            HasToBeOnEdge = hasToBeOnEdge;
            AvoidRoomTypes = avoidRoomTypes;
            MergeWith = mergeWith;
            RoomType = roomType;
            IsDoorSatisfied = false;
        }

        public AbstractRoom(RoomType roomType, int area, float squareness, bool hasToBeOnEdge, int areaMax,
            float squarenessDifferenceAllowed, RoomType[] avoidRoomTypes, RoomType[] mergeWith)
        {
            RoomType = roomType;
            Area = area;
            AreaMax = areaMax;
            Squareness = squareness;
            SquarenessDifferenceAllowed = squarenessDifferenceAllowed;
            HasToBeOnEdge = hasToBeOnEdge;
            AvoidRoomTypes = avoidRoomTypes;
            MergeWith = mergeWith;
            RoomType = roomType;
            IsDoorSatisfied = false;
        }
    }
}