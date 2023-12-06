using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Shared.ProceduralGeneration.Util
{
    public class WallInfo
    {
        public HouseRoomAssigner.RoomInfo Parent;
        public HouseRoomAssigner.RoomInfo Child;
        public bool Horizontal;
        public List<(int, int)> Points = new List<(int, int)>();
        public int Length => Points.Count;

        private void MakeAllWalls(Map map, float thickness)
        {
            foreach (var point in Points)
            {
                Texture color = new Texture("Wall",Color.Black);
                map.PaintWall(color, point.Item1, point.Item2, false, false, thickness);
            }
        }

        private void MakeDoorInMiddle(Map map, bool door = true)
        {
            Texture color = door ? new Texture("Door", Color.Yellow) : new Texture("Empty", Color.FromArgb(0,0,0,0));

            int middle = Length / 2;
            bool isPair = Length % 2 == 0;
            // paint middle wall as transparent
            if (Horizontal)
            {
                map.PaintWall(color, Points[middle].Item1, Points[middle].Item2);
                if (isPair)
                {
                    map.PaintWall(color, Points[middle - 1].Item1, Points[middle - 1].Item2);
                }
            }
            else
            {
                map.PaintWall(color, Points[middle].Item1, Points[middle].Item2);
            }
        }

        private void ColorAllWalls(Map map, Texture color)
        {
            foreach (var point in Points)
            {
                map.PaintWall(color, point.Item1, point.Item2);
            }
        }

        private void CreateHoleOf(Map map, int c)
        {
            bool isPair = Length % 2 == 0;
            int middle = Length / 2;
            int half = c / 2 + (isPair ? 1 : 0);
            int start = middle - half;
            int end = middle + half;

            if (end == Length)
                end--;

            for (int i = start; i <= end; i++)
            {
                if (i == start || i == end)
                {
                    map.PaintWall(new Texture("Wall", Color.Black), Points[i].Item1, Points[i].Item2, true, i == start);
                }
                else
                {
                    map.PaintWall( new Texture("Empty", Color.FromArgb(0,0,0,0)), Points[i].Item1, Points[i].Item2);
                }
            }
        }

        private void CreateComplexDoor(Map map)
        {
            switch (Length)
            {
                case 1:
                    ColorAllWalls(map, new Texture("Empty", Color.FromArgb(0,0,0,0)));
                    break;
                case 2:
                    break;
                    CreateHoleOf(map, 2);
                default:
                    CreateHoleOf(map, 2);
                    break;
            }
        }

        public void MakeWallsAndDoors(Map map, HouseGenerator.HouseBuilder houseGenerator)
        {

            if (Child == null)
                MakeAllWalls(map, 1);
            else
                MakeAllWalls(map, .3f);


            if (Parent.RoomType == RoomType.LivingRoom || Parent.RoomType == RoomType.Hall)
            {
                if (Child == null)
                {
                    MakeDoorInMiddle(map);
                }
                else if (Child.RoomType == RoomType.Hall || Child.RoomType == RoomType.LivingRoom)
                {
                    MakeDoorInMiddle(map);
                }
            }

            if (Parent.RoomType == RoomType.Garage)
            {
                if (Child == null)
                {
                    ColorAllWalls(map, new Texture("Garage", Color.FromArgb(0,0,0,0)));
                }
            }

            if (Child == null)
                return;

            if (Parent.RoomType == RoomType.Secret || Child.RoomType == RoomType.Secret)
            {
                ColorAllWalls(map, new Texture("Secret", Color.FromArgb(0,0,0,0)));
                return;
            }

            if (Parent.AbstractRoom.MergeWith != null)
            {
                if (Parent.AbstractRoom.MergeWith.Contains(Child.RoomType))
                {
                    CreateComplexDoor(map);
                    return;
                }
            }
            else if (Child.AbstractRoom.MergeWith != null)
            {
                if (Child.AbstractRoom.MergeWith.Contains(Parent.RoomType))
                {
                    CreateComplexDoor(map);
                    return;
                }
            }

            if (Parent.AbstractRoom.AvoidRoomTypes != null)
            {
                if (Parent.AbstractRoom.AvoidRoomTypes.Contains(Child.RoomType))
                {
                    return;
                }
            }
            else if (Child.AbstractRoom.AvoidRoomTypes != null)
            {
                if (Child.AbstractRoom.AvoidRoomTypes.Contains(Parent.RoomType))
                {
                    return;
                }
            }

            if (Parent.RoomType == Child.RoomType)
            {
                ColorAllWalls(map, new Texture("Empty", Color.FromArgb(0,0,0,0)));
                return;
            }


            // Si tienen un vecino en comun que sea un pasillo, no se pone puerta. Pone solo en pasillos
            bool hasConnectionWithHallway = false;
            if (Parent.Neighbours != null && Child.Neighbours != null)
            {
                foreach (var parentNeighbour in Parent.Neighbours)
                {
                    foreach (var childNeighbour in Child.Neighbours)
                    {
                        if (childNeighbour == parentNeighbour)
                        {
                            if (parentNeighbour.RoomType == RoomType.Hallway)
                            {
                                hasConnectionWithHallway = true;
                                break;
                            }
                        }
                    }
                }

                if (Parent.AbstractRoom.AvoidRoomTypes != null)
                {
                    if (Parent.AbstractRoom.AvoidRoomTypes.Contains(RoomType.Hallway))
                    {
                        hasConnectionWithHallway = false;
                    }
                }
                else if (Child.AbstractRoom.AvoidRoomTypes != null)
                {
                    if (Child.AbstractRoom.AvoidRoomTypes.Contains(RoomType.Hallway))
                    {
                        hasConnectionWithHallway = false;
                    }
                }
            }


            if (Parent.RoomType == RoomType.Hallway || Child.RoomType == RoomType.Hallway)
            {
                (int, int) HallwayPrefereedPosition;
                if (Parent.RoomType == RoomType.Hallway)
                {
                    HallwayPrefereedPosition = Parent.PreferredDoorPosition;
                }
                else
                {
                    HallwayPrefereedPosition = Child.PreferredDoorPosition;
                }

                Point2D point2D = new Point2D((HallwayPrefereedPosition.Item1 * 2 + 1,
                    HallwayPrefereedPosition.Item2 * 2 + 1));
                foreach (var valueTuple in Points)
                {
                    Point2D point2D1 = new Point2D(valueTuple.Item1, valueTuple.Item2);
                    if (point2D.DistanceTo(point2D1) < 2)
                    {
                        map.PaintWall(new Texture("Door", Color.Yellow), valueTuple.Item1, valueTuple.Item2);

                        if (Length < 7)
                            return;

                        MakeDoorInMiddle(map);
                    }
                }
            }

            if (hasConnectionWithHallway)
            {
                return;
            }


            MakeDoorInMiddle(map);
        }
    }
}