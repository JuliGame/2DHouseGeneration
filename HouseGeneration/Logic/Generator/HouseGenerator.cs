using System;
using System.Collections.Generic;
using System.Linq;
using HouseGeneration.Logic.Generator.@class;
using Microsoft.Xna.Framework;

namespace HouseGeneration.Logic;

public class HouseGenerator {

    public static Random random;
    
    public class HouseBuilder {
        
        private int[,] tileGrid;
        // 0 = outside, 1 = free, 2 = occupied, 3 = occupied_by_living_or_hallway
        private Room[,] tileRooms;
        private Room livingRoom;
        private Map map;
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
                            break;
                        }
                        
                        if (shouldFind.Contains(tileGrid[dx, dy]))
                        {
                            if (findClosest)
                            {
                                if (distance < bestDistance)
                                {
                                    // Console.Out.WriteLine(bestDistance);
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
        public RayCastResult Raycast((int, int) start, bool findClosest, int[] canTravelThrough, int[] shouldFind) {
            List<Side> sides = new List<Side>();
            sides.Add(Side.Top);
            sides.Add(Side.Right);
            sides.Add(Side.Bottom);
            sides.Add(Side.Left);
            
            ((int, int), (int, int), Side, int) best = default;
            int bestDistance = findClosest ? int.MaxValue : int.MinValue;
            
            foreach (var side in sides) {
                int distance = 0;
                while (true) {
                    int x = start.Item1;
                    int y = start.Item2;
                    
                    distance++;
                    int dx = x + (int)(side.GetX() * distance);
                    int dy = y + (int)(side.GetY() * distance);

                    if (dx < 0 || dx >= tileGrid.GetLength(0) || dy < 0 || dy >= tileGrid.GetLength(1)) {
                        break;
                    }
                    
                    if (shouldFind.Contains(tileGrid[dx, dy]))
                    {
                        if (findClosest)
                        {
                            if (distance < bestDistance)
                            {
                                // Console.Out.WriteLine(bestDistance);
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
                        break;
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
            // se inicia la generacion con un living room
            // lo primero, el living mide un "size"% del total de la casa
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
                livingRoom.Text = "living";
                livingRoom.Id = 3;  
                
                size -= 0.01f;
            }
            
            
            // Paso 2: Crear pasillos desde el living room
            // Una vez creado el living room, se crean un pasillo desde el living room a la salida mas lejana
            if (getFreeM2() > 10 * 5) {
                RayCastResult pasilloPrincipalRaycast = Raycast(livingRoom, false, new[] { 1 }, new[] { -1, 0 });
                if (pasilloPrincipalRaycast == null)
                    return false;
                
                Room hallway = CreateHallway(livingRoom, pasilloPrincipalRaycast.side, 5, true);
                if (hallway == null) {
                    return false;
                }
                
                hallway.Color = Color.Brown;
                hallway.Id = 3;
                TryToPlaceRoom(hallway, true);
            }
            
            // Paso 3: Crear pasillo del living a la entrada
            // Aca, si el living no esta pegado con una pared, se crea un pasillo desde el living a la entrada
            RayCastResult hallRaycast = Raycast(livingRoom, true, new[] { 1 }, new[] { -1, 0 });
            if (hallRaycast == null)
                return false;

            if (hallRaycast.distance > 1) {
                Room hall = hallRaycast.GetAsRoom(false, false);
                hall.Color = Color.Brown;
                hall.Text = "hall";
                hall.Id = 3;
                TryToPlaceRoom(hall, true);
            }
            
            // Paso 4: Crear puerta del living al pasillo
            // Aca, si el living no esta pegado con una pared, se crea un pasillo desde el living a la entrada
            Room door = new Room(1,1,hallRaycast.end.Item1, hallRaycast.end.Item2);
            door.Extend(hallRaycast.side.Invert());
            door.UnExtend(hallRaycast.side);
            door.Color = Color.Gray;
            door.Text = "door";
            door.Id = 3;
            TryToPlaceRoom(door, true);
            
            
            // todo crear AbstractRoom que sirva para tener info de la room pero en si no exista.
            // muy util para generar y hacer reglas y pelotudeces.
            // todo hacer generacion de habitaciones con un wave function collapse y las reglas de las habitaciones

            // Room garage = new Room(22, .2f, true);
            // Room kitchen = new Room(8, .3f, false);
            // Room bathroom = new Room(4, .3f, false);
            // Room bedroom = new Room(8, .3f, false);
            // garage.Color = Color.Gray;
            // garage.Text = "garage";
            // kitchen.Color = Color.Yellow;
            // kitchen.Text = "kitchen";
            // bathroom.Color = Color.Pink;
            // bathroom.Text = "bathroom";
            // bedroom.Color = Color.Blue;
            // bedroom.Text = "bedroom";
            //
            // List<Room> fundamentalRooms = new List<Room>();
            // fundamentalRooms.Add(garage);
            // fundamentalRooms.Add(kitchen);
            // fundamentalRooms.Add(bathroom);
            // fundamentalRooms.Add(bedroom);
            
            // Primera etapa de generacion de habitaciones 
            List<Room> placedRooms = new List<Room>(); 
            
            // Big rooms pass.
            // Aca se crean habitaciones grandes, llenando el espacio que queda libre
            List<List<(int, int)>> bubbles = GetBubbles();
            foreach (var bubble in bubbles) {
                Room b = new Room(bubble);
                List<Room> bRooms = b.Grow(4, this);
                
                foreach (var bRoom in bRooms) {
                    bRoom.Text = "bRoom";
                    if (TryToPlaceRoom(bRoom))
                        placedRooms.Add(bRoom);
                }
            }
            
            // Big fill rooms pass
            // Si quedan espacios libres, se crean habitaciones
            while (GetBubbles().Count >= 1) {
                Room b = new Room(GetBubbles()[0]);
                List<Room> bRooms = b.Grow(random.Next(1, 1), this);
            
                foreach (var sRoom in bRooms) {
                    sRoom.Text = "sRoom";
                    if (TryToPlaceRoom(sRoom))
                        placedRooms.Add(sRoom);
                }
                
            }


            // Hallway maze pass.
            // Ahora mismo esta to.do lleno, pero, puede que hayan habitaciones muy grandes
            // El hallway maze pass intenta dividir las habitaciones grandes en 2, poniento un pasillo en el medio
            foreach (var placedRoom in placedRooms.ToList()) {
                if ((placedRoom.Height > 6 || placedRoom.Width > 6) &&
                    placedRoom.points.Count >= livingRoom.points.Count) {
                    placedRooms.Remove(placedRoom);
                    Destroy(placedRoom);
                }
            }
            
            bool hasChanged = true;
            List<List<(int, int)>> bubbles2 = GetBubbles();
            while (hasChanged) {
                hasChanged = false;

                foreach (var bubble in bubbles2.ToList()) {
                    Room b = new Room(bubble);
                    
                    List<(int, int)> middles = b.GetMiddles();
                    // middles.Shuffle();
                    RayCastResult furthest = null;
                    foreach (var middle in middles) {
                        Room point = new Room(1, 1, middle.Item1, middle.Item2, false, Color.Red);
                        point.Id = 1;
                        TryToPlaceRoom(point);
                        
                        
                        RayCastResult raycast = Raycast(middle, true, new[] { 1 }, new[] { 3 });
                        if (raycast == null)
                            continue;
                        
                        if (raycast.distance == 1)
                            continue;
                            
                        if (furthest == null || raycast.distance > furthest.distance) {
                            furthest = raycast;
                        }
                    }
                    
                    if (furthest != null) {
                        Room hallway = furthest.GetAsRoom(true, false);
                        if (hallway.points.Count == 0)
                            continue;
                    
                        hallway.Color = Color.Brown;
                        hallway.Id = 3;
                        hallway.Text = "77";
                        TryToPlaceRoom(hallway, true);
                        hasChanged = true;
                        bubbles2.Remove(bubble);
                    }
                }
            }
            
            // Una vez creado el pasillo, se crean habitaciones en los espacios libres
            while (GetBubbles().Count >= 1) {
                Room b = new Room(GetBubbles()[0]);
                b.Text = "b";

                if (b.points.Count >= livingRoom.points.Count) {
                    
                    int slice = random.Next(2, 3);
                    int roomSize = b.Width + b.Height / slice;
                    
                    List<Room> bRooms = b.Grow(random.Next(2, 3), this, roomSize);
                    foreach (var sRoom in bRooms) {
                        sRoom.Text = "xRoom";
                        if (TryToPlaceRoom(sRoom))
                            placedRooms.Add(sRoom);
                    }
                }
                else {
                    TryToPlaceRoom(b);
                }
            }
            
            UpdateMapPaint();
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
                tileGrid[dx, dy] = room.Id;
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
                        if (tileGrid[x, y] >= 2) {
                            if (new Point2D((x, y)).DistanceTo(new Point2D(((int, int)) room.GetCenter())) < 1) {
                                map.Paint(room.Color, x, y, room.Text + $" ({room.Id})");
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
            UpdateMapPaint();
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
    public static void Generate(Map map, int seed) {
        random = new Random(seed);

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
            // map.Generate();
            return;
        }
    }
    
    
}