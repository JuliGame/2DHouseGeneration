using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace HouseGeneration.Logic;

public class HouseGenerator
{
    public static Random random = new Random();
    
    public class HouseBuilder {
        
        private int[,] tileGrid;
        // 0 = outside, 1 = free, 2 = occupied, 3 = occupied_but_accessible
        private Room[,] tileRooms;
        private Room livingRoom;
        private Map map;
        
        private void printTileGrid() {
            for (int y = 0; y < tileGrid.GetLength(1); y++) {
                for (int x = 0; x < tileGrid.GetLength(0); x++) {
                    Console.Out.Write(tileGrid[x, y] + " ");
                }
                Console.Out.WriteLine();
            }
        }
        public HouseBuilder(Map map, PolygonGenerator.TileInfo[,] tiles, int margin) {
            tileGrid = new int[tiles.GetLength(0) + margin * 2, tiles.GetLength(1) + margin * 2];
            for (int x = -this.margin; x < tileGrid.GetLength(0) - margin; x++) {
                for (int y = -this.margin; y < tileGrid.GetLength(1) - margin; y++) {
                    if (x < 0 || x >= tiles.GetLength(0) || y < 0 || y >= tiles.GetLength(1)) {
                        tileGrid[x + margin, y + margin] = -1;
                        continue;
                    }

                    tileGrid[x + margin, y + margin] = tiles[x, y].IsFilled ? 1 : 0;
                }
            }
            
            tileRooms = new Room[tiles.GetLength(0) + margin * 2, tiles.GetLength(1) + margin * 2];
            this.map = map;
            this.margin = margin;
        }
        
        private int getTile(int x, int y) {
            if (x < 0 || x >= tileGrid.GetLength(0) || y < 0 || y >= tileGrid.GetLength(1)) {
                return 0;
            }
            return tileGrid[x, y];
        }
        
        private int getTile((int, int) point) {
            return getTile(point.Item1, point.Item2);
        }
        
        private int getTile(Point2D point) {
            return getTile(point.X, point.Y);
        }
        
        public int getFreeM2() {
            int freeM2 = 0;
            foreach (var isFree in tileGrid) {
                freeM2 += isFree == 1 ? 1 : 0;
            }
            return freeM2;
        }

        public class RayCastResult {
            public (int, int) root;
            public (int, int) end;
            public Side side;
            public int distance;
            public Room GetAsRoom(bool includeIn, bool includeOut) {
                List<(int, int)> points = new List<(int, int)>();
                int dx = root.Item1;
                int dy = root.Item2;
                for (int i = 0; i < distance; i++) {
                    points.Add((dx, dy));
                    dx += (int)side.GetX();
                    dy += (int)side.GetY();
                }
                if (!includeIn) {
                    points.RemoveAt(0);
                }
                if (includeOut) {
                    points.Add(end);
                }
                return new Room(points);
            }
        }

        public RayCastResult Raycast(Room room, bool findClosest, int[] canTravelThrough, int[] shouldFind) {
            List<Side> sides = new List<Side>();
            sides.Add(Side.Top);
            sides.Add(Side.Right);
            sides.Add(Side.Bottom);
            sides.Add(Side.Left);
            
            ((int, int), (int, int), Side, int) best = default;
            int bestDistance = findClosest ? int.MaxValue : int.MinValue;
            
            foreach (var side in sides) {
                List<(int, int)> border = room.GetPoinsOfSide(side);

                foreach (var (x, y) in border)
                {
                    int distance = 0;
                    while (true)
                    {
                        distance++;
                        int dx = x + (int)(side.GetX() * distance);
                        int dy = y + (int)(side.GetY() * distance);

                        if (dx < 0 || dx >= tileGrid.GetLength(0) || dy < 0 || dy >= tileGrid.GetLength(1)) {
                            Console.Out.WriteLine("1");
                            break;
                        }
                        
                        if (shouldFind.Contains(tileGrid[dx, dy]))
                        {
                            if (findClosest)
                            {
                                if (distance < bestDistance)
                                {
                                    Console.Out.WriteLine(bestDistance);
                                    bestDistance = distance;
                                    best = ((x, y), (dx, dy), side, distance);
                                }
                                break;
                            }
                            else
                            {
                                if (distance > bestDistance)
                                {
                                    bestDistance = distance;
                                    best = ((x, y), (dx, dy), side, distance);
                                }
                            }
                        }
                        
                        if (!canTravelThrough.Contains(tileGrid[dx, dy])) {
                            Console.Out.WriteLine("2   " + tileGrid[dx, dy]);
                            break;
                        }
                        
                        // Room r = new Room(1,1,dx,dy);
                        // r.Color = Color.Red;
                        // r.Text = distance.ToString();
                        // TryToPlaceRoom(r, true);
                    }
                }
            }

            return best == default ? null : new RayCastResult() {
                root = best.Item1,
                end = best.Item2,
                side = best.Item3,
                distance = best.Item4
            };
        }
        

        public bool BuildHouse()
        {
            // Paso 1: Crear Living room
            float size = 0.2f;
            livingRoom = null;
            while (livingRoom == null || !AddRoomRandomly(livingRoom)) {
                if (size < 0.1f) {
                    return false;
                }
                
                int freeM2 = getFreeM2();
                int roomM2 = (int) (freeM2 * size);
                float noSquareness = random.Next(30) / 100f;
                noSquareness *= (random.Next(20) - 10f)/ 100f; 
                livingRoom = new Room(roomM2, noSquareness, false, Color.SaddleBrown);
                
                size -= 0.01f;
            }
            livingRoom.Text = "living";
            
            
            
            // Paso 2: Crear pasillos desde el living room
            if (getFreeM2() > 10 * 5) {
                RayCastResult pasilloPrincipalRaycast = Raycast(livingRoom, false, new[] { 1 }, new[] { -1, 0 });
                if (pasilloPrincipalRaycast == null)
                    return false;
                
                Room hallway = CreateHallway(livingRoom, pasilloPrincipalRaycast.side, 5, true);
                if (hallway == null) {
                    return false;
                }
                
                hallway.Color = Color.Brown;
                
                TryToPlaceRoom(hallway, true);
            }
            
            // Paso 3: Crear pasillo del living a la entrada
            
            RayCastResult hallRaycast = Raycast(livingRoom, true, new[] { 1 }, new[] { -1, 0 });
            if (hallRaycast == null)
                return false;

            if (hallRaycast.distance > 1) {
                Room hall = hallRaycast.GetAsRoom(false, false);
                hall.Color = Color.Brown;
                hall.Text = "hall";
                TryToPlaceRoom(hall, true);
            }
            
            Room door = new Room(1,1,hallRaycast.end.Item1, hallRaycast.end.Item2);
            door.Extend(hallRaycast.side.Invert());
            door.UnExtend(hallRaycast.side);
            door.Color = Color.Gray;
            door.Text = "door";
            TryToPlaceRoom(door, true);
            

            float sizeDifference = 0;
            List<List<(int, int)>> Bubbles = GetBubbles();
            // Calculamos la diferencia de tamaño entre la habitación más grande y la más pequeña
            foreach (var bubble in Bubbles) {
                int minX = bubble.Min(point => point.Item1);
                int maxX = bubble.Max(point => point.Item1);
                int minY = bubble.Min(point => point.Item2);
                int maxY = bubble.Max(point => point.Item2);
                int width = maxX - minX;
                int height = maxY - minY;
                sizeDifference = Math.Max(sizeDifference, Math.Abs(width - height));
            }
            
            Console.Out.WriteLine(Bubbles.Count + " Size difference: " + sizeDifference);

            Room garage = new Room(22, .2f, true);
            Room kitchen = new Room(8, .3f, false);
            Room bathroom = new Room(4, .3f, false);
            Room bedroom = new Room(8, .3f, false);
            garage.Color = Color.Gray;
            garage.Text = "garage";
            kitchen.Color = Color.Yellow;
            kitchen.Text = "kitchen";
            bathroom.Color = Color.Pink;
            bathroom.Text = "bathroom";
            bedroom.Color = Color.Blue;
            bedroom.Text = "bedroom";
            
            
            // todo crear AbstractRoom que sirva para tener info de la room pero en si no exista.
            // muy util para generar y hacer reglas y pelotudeces.
            
            
            // todo hacer generacion de habitaciones con un wave function collapse y las reglas de las habitaciones
            
            // List<Room> fundamentalRooms = new List<Room>();
            // fundamentalRooms.Add(garage);
            // fundamentalRooms.Add(kitchen);
            // fundamentalRooms.Add(bathroom);
            // fundamentalRooms.Add(bedroom);
            //
            //
            // List<Room> placedRooms = new List<Room>();
            // int index = 0;
            // int attempts = 0;
            // while (placedRooms.Count < fundamentalRooms.Count) {
            //     attempts++;
            //     
            //     if (attempts > 1000)
            //         return false;
            //     
            //     Room room = fundamentalRooms[index];
            //     if (!AddRoomRandomly(room)) {
            //         foreach (var placedRoom in placedRooms) {
            //             Destroy(placedRoom);
            //         }
            //         placedRooms.Clear();
            //         index = 0;
            //         continue;
            //     }
            //     placedRooms.Add(room);
            //     index++;
            // }
            //
            //
            // Console.Out.WriteLine("Generacion 20%");
            // if (getFreeM2() > 10) {
            //     Room bedroomExtra = new Room(6, .3f, false);
            //     bedroomExtra.Color = Color.Blue;
            //     bedroomExtra.Text = "bedroom e";
            //     
            //     Room almacen = new Room(9, 0f, false);
            //     bedroomExtra.Color = Color.Blue;
            //     bedroomExtra.Text = "bat e";
            //     
            //     Room bathroomExtra = new Room(2, 1f, false);
            //     bedroomExtra.Color = Color.Blue;
            //     bedroomExtra.Text = "bat e";
            //     
            //     AddRoomRandomly(bedroomExtra, false);
            //     AddRoomRandomly(almacen, false);
            //     AddRoomRandomly(bathroomExtra, false);
            // }

            List<List<(int, int)>> bubbles = GetBubbles();
            foreach (var bubble in bubbles) {
                Room b1 = new Room(bubble);
                Room b1_1 = b1.Grow(4, this);
                // b1.Text = "b1";
                // TryToPlaceRoom(b1);
            
                if (b1_1 != null) {
                    TryToPlaceRoom(b1_1);
                }
            }
            

            
            
            Console.Out.WriteLine("Generacion 1000%%%%%");
            
            
            // Paso 3: Crear habitaciones desde los pasillos
            // List<List<(int, int)>> bubbles = GetBubbles();
            // foreach (var bubble in bubbles) {
            //     Room room = new Room(bubble);
            //     if (!CanPlaceRoomAt(room))
            //         continue;
            //
            //     room.Text = "room";
            //     // TryToPlaceRoom(room);
            //
            //     // Paso 4: Dividir habitaciones si es posible
            //     List<Room> divition = room.Divide(2);
            //     Console.Out.WriteLine(divition.Count);
            //     foreach (var room1 in divition) {
            //         if (!TryToPlaceRoom(room1, true))
            //             System.Console.WriteLine("No se pudo colocar la habitación");
            //     }
            // }
            
            
            return true;
        }
        
        # region Rooms
        public bool AddRoomRandomly(Room room, bool force = false) {
            // Random Approach
            int attempts = 10;
            for (int i = 0; i < attempts; i++) {
                int x = random.Next(tileGrid.GetLength(0));
                int y = random.Next(tileGrid.GetLength(1));
                if (tileGrid[x, y] == 1) {
                    room.MoveTo(x, y);
                    if (!TryToPlaceRoom(room))
                        continue;
                    
                    return true;
                }
            }
            
            // Brute Force Approach
            for (int x = 0; x < room.Width; x++) {
                for (int y = 0; y < room.Height; y++) {
                    room.MoveTo(x, y);
                    if (!TryToPlaceRoom(room))
                        continue;
                    
                    return true;
                }
            }
            
            if (force) {
                
            }

            return false;
        }
        public bool TryToPlaceRoom(Room room, bool force = false) {
            if (!CanPlaceRoom(room) && !force)
                return false;
            
            foreach (var (dx, dy) in room.points) {
                tileGrid[dx, dy] = 2;
                tileRooms[dx, dy] = room;
            }
            
            UpdateMapPaint();
            return true;
        }

        int margin = 2;
        private void UpdateMapPaint() {
            for (int x = 0; x < tileGrid.GetLength(0); x++) {
                for (int y = 0; y < tileGrid.GetLength(1); y++) {
                    Room room = getRoom(x, y);
                    if (room != null) {
                        if (tileGrid[x, y] == 2) {
                            if (new Point2D((x, y)).DistanceTo(new Point2D(((int, int)) room.GetCenter())) < 1) {
                                map.Paint(room.Color, x, y, room.Text);
                            }
                            else {
                                map.Paint(room.Color, x, y);
                            }
                        }
                        else {
                            map.Paint(Color.Green, x, y);
                        }
                    }
                    else {
                        if (tileGrid[x, y] == -1 || tileGrid[x, y] == 0) {
                            map.Paint(Color.Green, x, y);
                        }
                        else {
                            map.Paint(Color.Black, x, y);
                        }
                    }
                }
            }
        }


        public bool CanPlaceRoom(Room room) {
            foreach (var (dx, dy) in room.points) {
                try {
                    if (tileGrid[dx, dy] != 1) {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    return false;
                }
            }
            return true;
        }

        public void Destroy(Room room)
        {
            foreach (var valueTuple in room.points) {
                tileGrid[valueTuple.Item1, valueTuple.Item2] = 1;
                tileRooms[valueTuple.Item1, valueTuple.Item2] = null;
            }
        }
        
        #endregion
        
        #region Holes

        private int RecursivePaint(int[,] tiles, int x, int y) {
            List<(int, int)> freeHoles = GetFreeHoles(tiles, x, y, new List<(int, int)>());
            foreach (var freeHole in freeHoles) {
                tiles[freeHole.Item1, freeHole.Item2] = 6;
            }
            return freeHoles.Count;
        }
        
        private List<(int, int)> GetFreeHoles(int[,] tiles, int x, int y, List<(int, int)> added) {
            if (x < 0 || x >= tiles.GetLength(0) || y < 0 || y >= tiles.GetLength(1)) {
                return added;
            }
            
            if (tiles[x, y] != 1) {
                return added;
            }
            
            tiles[x, y] = 6;
            added.Add((x, y));
            
            GetFreeHoles(tiles, x + 1, y, added);
            GetFreeHoles(tiles, x - 1, y, added);
            GetFreeHoles(tiles, x, y + 1, added);
            GetFreeHoles(tiles, x, y - 1, added);
            
            return added;
        }

        private List<int> GetFreeHolesSizes() {
            List<int> freeHolesSizes = new List<int>();
            int[,] tiles = this.tileGrid.Clone() as int[,];
            
            for (int x = 0; x < tileGrid.GetLength(0); x++) {
                for (int y = 0; y < tileGrid.GetLength(1); y++) {
                    if (tiles[x, y] == 1) {
                        int freeHoles = RecursivePaint(tiles, x, y);
                        freeHolesSizes.Add(freeHoles);
                    }
                }
            }
            return freeHolesSizes;
        }

        public List<List<(int, int)>> GetBubbles() {
            List<List<(int, int)>> bubbles = new List<List<(int, int)>>();
            int[,] tiles = this.tileGrid.Clone() as int[,];
            
            // use method GetFreeHoles to get the bubbles (all different free holes)
            for (int x = 0; x < tileGrid.GetLength(0); x++) {
                for (int y = 0; y < tileGrid.GetLength(1); y++) {
                    if (tiles[x, y] == 1) {
                        List<(int, int)> bubble = GetFreeHoles(tiles, x, y, new List<(int, int)>());
                        bubbles.Add(bubble);
                    }
                }
            }
            return bubbles;
        }

        #endregion
        
        #region Hallways
        private Room CreateHallway(Room room, Side facingSide, int lenght, bool tryAvoidingCorners = false) {
            int x, y;
            List<(int, int)> points = room.GetPoinsOfSide(facingSide);
            if (points.Count == 0) {
                return null;
            }

            if (points.Count > 2 && tryAvoidingCorners) {
                List<(int, int)> finalPoints = new List<(int, int)>();
                for (int i = 0; i < points.Count; i++)
                {
                    if (i != 0 && i != points.Count - 1) {
                        finalPoints.Add(points[i]);
                    }
                }
                points = finalPoints;
            }
            
            int wallSize = points.Count + (tryAvoidingCorners ? 2 : 0);
            
            Room best = null;
            int bestWallPoints = 0;
         
            foreach (var point in points) {
                int wallPoints = 0;
                (x, y) = new Point2D(point).Extend(facingSide).ToTuple();
            
                Room hallway = new Room(1, 1, x, y, false, Color.Black);
                
                while (CanPlaceRoom(hallway)) {
                    hallway.Extend(facingSide);
                }
                hallway.UnExtend(facingSide);
                wallPoints += hallway.Width + hallway.Height - 1;
                
                if (hallway.Width + hallway.Height - 1 < lenght)
                    continue;
                
                // Sacamos los puntos si esta pegado a una pared
                Point2D roomTip = new Point2D(hallway.GetPoinsOfSide(facingSide)[0]);
                Side pSide1 = facingSide.GetPerpendicularSide(false);
                Side pSide2 = facingSide.GetPerpendicularSide(true);
                if (getTile(roomTip.Extend(pSide1)) == 0 || getTile(roomTip.Extend(pSide2)) == 0) 
                    wallPoints = (int) (wallPoints * .75f);

                if (TryToPlaceRoom(hallway, false)) {
                    List<List<(int, int)>> border = GetBubbles();
                    // Get smallest bubble
                    int smallestBubble = int.MaxValue;
                    foreach (var bubble in border) {
                        smallestBubble = Math.Min(smallestBubble, bubble.Count);
                    }
                    wallPoints += smallestBubble;
                
                    Destroy(hallway);
                }

                
                if (bestWallPoints == 0 || wallPoints >= bestWallPoints) {
                    if (bestWallPoints == wallPoints && random.Next(2) == 0)
                            continue;
                    

                    
                    best = hallway;
                    bestWallPoints = wallPoints;
                }
            }

            if (best != null) {
                Point2D roomTip = new Point2D(best.GetPoinsOfSide(facingSide)[0]);
                Side pSide1 = facingSide.GetPerpendicularSide(false);
                Side pSide2 = facingSide.GetPerpendicularSide(true);

                if (getTile(roomTip.Extend(pSide1)) == 0 && getTile(roomTip.Extend(pSide2)) == 0 && getTile(roomTip.Extend(facingSide)) == 0) {
                    best.UnExtend(facingSide);
                    
                    // todo mejorar esto con matematica
                    try {
                        tileGrid[roomTip.Extend(facingSide).X, roomTip.Extend(facingSide).Y] = 0;
                    }catch (Exception e) {
                        // Console.Out.WriteLine("Error: " + e);
                    }

                    Console.Out.WriteLine("Refactor: " + roomTip.X + ", " + roomTip.Y);
                }
            }
            
            return best;
        }
        
        #endregion
        public Room getRoom(int x, int y) {
            return tileRooms[x, y];
        }
    }
    public static void Generate(Map map) {
        int Margin = 2;
        Side enterSide = Side.Top;
        
        int random100 = random.Next(100);
        
        int sx = map.x - Margin * 2;
        int sy = map.y - Margin * 2;

        PolygonGenerator.TileInfo[,] tiles;

        // if (random100 < 33) {
        //     tiles = PolygonGenerator.CreatePolygon(sx, sy, 0.2);
        // }
        // else if (random100 < 66) {
        //     tiles = PolygonGenerator.CreatePolygon(sx, sy, 0.05);
        // }
        // else {
            tiles = PolygonGenerator.CreatePolygon(sx, sy, 0);
        // }
        
        HouseBuilder houseBuilder = new HouseBuilder(map, tiles, Margin);
        if (!houseBuilder.BuildHouse()) {
            Console.Out.WriteLine("No se pudo generar la casa");
            map.Generate();
            return;
        }


        // houseBuilder.AddRoom(garage);
        // houseBuilder.AddRoom(livingRoom);
        // houseBuilder.AddRoom(kitchen);
        // houseBuilder.AddRoom(bathroom);
        // houseBuilder.AddRoom(bedroom);
    }
    
    
}