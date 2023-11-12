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
    }

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

        
        PlaceRoom(new AbstractRoom(RoomType.Kitchen, 6, .3f, false));
        PlaceRoom(new AbstractRoom(RoomType.Bathroom, 2, .2f, false));
        PlaceRoom(new AbstractRoom(RoomType.Bedroom, 4, .3f, false));
        
        List<RoomInfo> placedRooms = PlaceRoomsRecursively(_toPlace, roomInfos);
        if (placedRooms == null || placedRooms.Count == 0)
            return null;

        Console.Out.WriteLine("placedRooms.Count = {0}", placedRooms.Count);
        PlaceRoom(new AbstractRoom(RoomType.Garage, 22, .3f, false));
        
        return placedRooms;
    }

    private bool CanBePlaced(AbstractRoom abstractRoom, RoomInfo roomInfo) {
        if (roomInfo.Room.points.Count < abstractRoom.Area)
            return false;
        
        if (roomInfo.Room.GetSquareness() < abstractRoom.Squareness)
            return false;
        
        if (!roomInfo.IsOnEdge && abstractRoom.HasToBeOnEdge)
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
                    IsOnEdge = availableRooms[i].IsOnEdge
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