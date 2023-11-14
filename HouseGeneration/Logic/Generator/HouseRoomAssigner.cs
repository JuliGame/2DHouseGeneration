using System;
using System.Collections.Generic;
using System.Linq;
using HouseGeneration.Logic.Generator.@class;

namespace HouseGeneration.Logic.Generator;

public class HouseRoomAssigner
{

    private Random _random;
    private HouseGenerator.HouseBuilder _builder;
    public HouseRoomAssigner(HouseGenerator.HouseBuilder builder) {
        _builder = builder;
        _random = HouseGenerator.Random;
    }

    private List<AbstractRoom> _toPlace = new List<AbstractRoom>();
    private void PlaceRoom(AbstractRoom room) {
        _toPlace.Add(room);
    }
    
    public class RoomInfo {
        public Room Room;
        public List<RoomInfo> Neighbours = new List<RoomInfo>();
        public Room Group;
        public RoomType RoomType = RoomType.Other;
        public bool IsOnEdge;
        public bool IsAlreadyPlaced = false;
        public AbstractRoom AbstractRoom;
    }

    private List<RoomInfo> _placedRooms;
    public List<RoomInfo> Assign(List<Room> rooms, List<RoomInfo> alreadyPlaced) {
        // Groups are the rooms that are connected to each other
        List<RoomInfo> roomInfos = new List<RoomInfo>();
        foreach (var room in rooms) {
            RoomInfo roomInfo = new RoomInfo();
            roomInfo.Room = room;
            roomInfos.Add(roomInfo);
        }
        
        roomInfos.AddRange(alreadyPlaced);
        SetGroupsAndNeighbours(roomInfos);

        AbstractRoom kitchen = new AbstractRoom(RoomType.Kitchen, 6, .3f, false, null, new [] { RoomType.LivingRoom });
        
        AbstractRoom bathroom = new AbstractRoom(RoomType.Bathroom, 2, .2f, false, 
            new [] { RoomType.Kitchen, RoomType.Garage, RoomType.Machines, RoomType.Storage },
            null);
        
        AbstractRoom bedroom = new AbstractRoom(RoomType.Bedroom, 4, .3f, false);
            
        PlaceRoom(kitchen);
        PlaceRoom(bathroom);
        PlaceRoom(bedroom);
        
        _placedRooms = PlaceRoomsRecursively(_toPlace, roomInfos);
        if (_placedRooms == null || _placedRooms.Count == 0)
            return null;

        Console.Out.WriteLine("placedRooms.Count = {0}", _placedRooms.Count);
        TryToPlaceRoom(new AbstractRoom(RoomType.Garage, 20, .3f, true), roomInfos);
        TryToPlaceRoom(new AbstractRoom(RoomType.Storage, 15, .3f, true, null , new [] { RoomType.Garage}), roomInfos);
        TryToPlaceRoom(new AbstractRoom(RoomType.Machines, 3, 0f, false, 6, 0f, null, new [] { RoomType.Garage, RoomType.Storage }), roomInfos);
        
        TryToPlaceRoom(new AbstractRoom(RoomType.Secret, 1, 0f, false, 1, 0f, null, null), roomInfos);
        return _placedRooms;
    }

    private void TryToPlaceRoom(AbstractRoom room, List<RoomInfo> availableRooms) {
        if (_placedRooms == null || _placedRooms.Count == 0)
            return;
        
        PlaceRoom(room);
        List<RoomInfo> rooms = PlaceRoomsRecursively(_toPlace, availableRooms);
        if (rooms != null) {
            _placedRooms = rooms;
        }
    }

    private bool CanBePlaced(AbstractRoom abstractRoom, RoomInfo roomInfo) {
        if (roomInfo.Room.points.Count < abstractRoom.Area)
            return false;
        
        if (roomInfo.Room.GetSquareness() < abstractRoom.Squareness)
            return false;
        
        if (!roomInfo.IsOnEdge && abstractRoom.HasToBeOnEdge)
            return false;
        
        if (abstractRoom.AreaMax < roomInfo.Room.points.Count)
            return false;
        
        bool canPlaceDoor = false;
        if (abstractRoom.AvoidRoomTypes != null) {
            foreach (var neighbour in roomInfo.Neighbours) {
                if (abstractRoom.AvoidRoomTypes.Contains(neighbour.RoomType)) 
                    continue;
            
                canPlaceDoor = true;
                break;
            }
        } else {
            canPlaceDoor = true;
        }

        if (!canPlaceDoor)
            return false;
        
        
        
        return true;
    }

    private List<RoomInfo> PlaceRoomsRecursively(List<AbstractRoom> roomsToPlace, List<RoomInfo> availableRooms, int currentRoomIndex = 0) 
    {
        // Base case: If we've successfully placed all rooms
        if (currentRoomIndex >= roomsToPlace.Count) {
            return new List<RoomInfo>(availableRooms);
        }
        
        if (currentRoomIndex == 0) {
            roomsToPlace = roomsToPlace.OrderBy(x => _random.Next()).ToList();
            availableRooms = availableRooms.OrderBy(x => _random.Next()).ToList();
        }

        var roomToPlace = roomsToPlace[currentRoomIndex];

        // Try placing the current room at each available position
        for (int i = 0; i < availableRooms.Count; i++) {
            
            if (availableRooms[i].IsAlreadyPlaced)
                continue;
            
            if (CanBePlaced(roomToPlace, availableRooms[i]))
            {
                // Temporarily place the room
                RoomInfo roomInfo = new RoomInfo() {
                    Room = availableRooms[i].Room,
                    RoomType = roomToPlace.RoomType,
                    Neighbours = availableRooms[i].Neighbours,
                    Group = availableRooms[i].Group,
                    IsOnEdge = availableRooms[i].IsOnEdge,
                    IsAlreadyPlaced = true,
                    AbstractRoom = roomToPlace
                };
                
                availableRooms.RemoveAt(i);

                // Recurse with the next room
                var result = PlaceRoomsRecursively(roomsToPlace, availableRooms, currentRoomIndex + 1);

                // If we found a valid placement for the rest of the rooms, return it
                if (result != null) {
                    result.Add(roomInfo);
                    return result;
                }

                // Otherwise, undo the placement and continue with the next possibility
                availableRooms.Insert(i, roomInfo);
            }
        }

        // If we've tried all possibilities and none worked, there is no solution
        return null;
    }

    
    
    // UTILS
    public bool AreNeighbours((int, int) a, (int, int) b) {
        if (a.Item1 == b.Item1) {
            if (a.Item2 == b.Item2 + 1 || a.Item2 == b.Item2 - 1) {
                return true;
            }
        }
        if (a.Item2 == b.Item2) {
            if (a.Item1 == b.Item1 + 1 || a.Item1 == b.Item1 - 1) {
                return true;
            }
        }
        return false;
    }

    public bool AreNeighbours(Room room1, Room room2) {
        foreach (var room1Point in room1.points) {
            foreach (var room2Point in room2.points) {
                if (AreNeighbours(room1Point, room2Point)) {
                    return true;
                }
            }
        }
        return false;
    }

    public List<List<RoomInfo>> SetGroupsAndNeighbours(List<RoomInfo> rooms)
    {
        List<List<RoomInfo>> groups = new List<List<RoomInfo>>();
        foreach (var roomInfo in rooms)
        {
            bool added = false;
            foreach (var group in groups)
            {
                foreach (var groupRoom in group.ToList())
                {
                    if (AreNeighbours(roomInfo.Room, groupRoom.Room)) {
                        group.Add(roomInfo);
                        roomInfo.Group = groupRoom.Group;
                        added = true;
                        break;
                    }
                }

                if (added)
                {
                    break;
                }
            }

            if (!added)
            {
                groups.Add(new List<RoomInfo>() { roomInfo });
            }

            foreach (var roomInfo2 in rooms) {
                if (roomInfo == roomInfo2) 
                    continue;
                
                if (AreNeighbours(roomInfo.Room, roomInfo2.Room)) {
                    if (roomInfo.Neighbours.Contains(roomInfo2)) 
                        continue;
                    
                    roomInfo.Neighbours.Add(roomInfo2);
                    roomInfo2.Neighbours.Add(roomInfo);
                }
            }

            HouseGenerator.HouseBuilder.RayCastResult result =
                _builder.Raycast(roomInfo.Room, true, new[] { 2 }, new[] { -1, 0 });

            
            roomInfo.IsOnEdge = result != null && result.Distance == 1;
        }
        return groups;
    }
}