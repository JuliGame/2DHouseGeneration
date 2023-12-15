using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using Shared.ProceduralGeneration;
using Shared.ProceduralGeneration.Util;

public class HouseGenerator {

    public static Random Random;
    
    public class HouseBuilder {
        
        private int[,] _tileGrid;
        // 0 = outside, 1 = free, 2 = occupied, 3 = occupied_by_living_or_hallway
        private Room[,] _tileRooms;
        private Room _livingRoom;
        private Map _map;
        public HouseBuilder(Map map, PolygonGenerator.TileInfo[,] tiles, int margin) {
            _tileGrid = new int[tiles.GetLength(0) + margin * 2, tiles.GetLength(1) + margin * 2];
            for (int x = -this._margin; x < _tileGrid.GetLength(0) - margin; x++) {
                for (int y = -this._margin; y < _tileGrid.GetLength(1) - margin; y++) {
                    if (x < 0 || x >= tiles.GetLength(0) || y < 0 || y >= tiles.GetLength(1)) {
                        _tileGrid[x + margin, y + margin] = -1;
                        continue;
                    }

                    _tileGrid[x + margin, y + margin] = tiles[x, y].IsFilled ? 1 : 0;
                }
            }
            
            _tileRooms = new Room[tiles.GetLength(0) + margin * 2, tiles.GetLength(1) + margin * 2];
            this._map = map;
            this._margin = margin;
        }
        
        public int GetTile(int x, int y) {
            if (x < 0 || x >= _tileGrid.GetLength(0) || y < 0 || y >= _tileGrid.GetLength(1)) {
                return 0;
            }
            return _tileGrid[x, y];
        }
        
        public int GetTile((int, int) point) {
            return GetTile(point.Item1, point.Item2);
        }
        
        public int GetTile(Point2D point) {
            return GetTile(point.X, point.Y);
        }
        
        public int GetFreeM2() {
            int freeM2 = 0;
            foreach (var isFree in _tileGrid) {
                freeM2 += isFree == 1 ? 1 : 0;
            }
            return freeM2;
        }

        public class RayCastResult {
            public (int, int) Root;
            public (int, int) End;
            public Side Side;
            public int Distance;
            public Room GetAsRoom(bool includeIn, bool includeOut) {
                List<(int, int)> points = new List<(int, int)>();
                int dx = Root.Item1;
                int dy = Root.Item2;
                for (int i = 0; i < Distance; i++) {
                    points.Add((dx, dy));
                    dx += Side.GetX();
                    dy += Side.GetY();
                }
                if (!includeIn) {
                    points.RemoveAt(0);
                }
                if (includeOut) {
                    points.Add(End);
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
                        int dx = x + side.GetX() * distance;
                        int dy = y + side.GetY() * distance;

                        if (dx < 0 || dx >= _tileGrid.GetLength(0) || dy < 0 || dy >= _tileGrid.GetLength(1)) {
                            break;
                        }
                        
                        if (shouldFind.Contains(_tileGrid[dx, dy]))
                        {
                            if (findClosest)
                            {
                                if (distance < bestDistance)
                                {
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
                        
                        if (!canTravelThrough.Contains(_tileGrid[dx, dy])) {
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
                Root = best.Item1,
                End = best.Item2,
                Side = best.Item3,
                Distance = best.Item4
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
                    int dx = x + side.GetX() * distance;
                    int dy = y + side.GetY() * distance;

                    if (dx < 0 || dx >= _tileGrid.GetLength(0) || dy < 0 || dy >= _tileGrid.GetLength(1)) {
                        break;
                    }
                    
                    if (shouldFind.Contains(_tileGrid[dx, dy]))
                    {
                        if (findClosest)
                        {
                            if (distance < bestDistance)
                            {
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
                    
                    if (!canTravelThrough.Contains(_tileGrid[dx, dy])) {
                        break;
                    }
                }
            }

            return best == default ? null : new RayCastResult() {
                Root = best.Item1,
                End = best.Item2,
                Side = best.Item3,
                Distance = best.Item4
            };
        }

        public bool BuildHouse()
        {
            // Paso 1: Crear Living room
            // se inicia la generacion con un living room
            // lo primero, el living mide un "size"% del total de la casa
            UpdateMapPaint();
            
            float size = 0.2f;
            int maxLivingRoomSize = 8*8;
            _livingRoom = null;
            while (_livingRoom == null || !AddRoomRandomly(_livingRoom)) {
                if (size < 0.1f) {
                    return false;
                }
                
                int freeM2 = GetFreeM2();
                int roomM2 = (int) (freeM2 * size);
                if (roomM2 > maxLivingRoomSize)
                    roomM2 = maxLivingRoomSize;
                
                float noSquareness = Random.Next(30) / 100f;
                noSquareness *= (Random.Next(20) - 10f)/ 100f; 
                _livingRoom = new Room(roomM2, noSquareness, false, new Texture("Ground", Color.SaddleBrown));
                _livingRoom.Text = "living";
                _livingRoom.Id = 3;  
                
                size -= 0.01f;
            }

            UpdateMapPaint();
            
            List<Room> hallways = new List<Room>();
            Dictionary<Room, (int, int)> hallwaysEnds = new Dictionary<Room, (int, int)>();
            // Paso 2: Crear pasillo del living a la entrada
            // Aca, si el living no esta pegado con una pared, se crea un pasillo desde el living a la entrada
            RayCastResult hallRaycast = Raycast(_livingRoom, true, new[] { 1 }, new[] { -1, 0 });
            if (hallRaycast == null)
                return false;

            Room hall = null;
            if (hallRaycast.Distance > 1) {
                hall = hallRaycast.GetAsRoom(false, false);
                hall.Texture = new Texture("Ground",Color.Brown);
                hall.Text = "hall";
                hall.Id = 3;
                TryToPlaceRoom(hall, true);
            }
            
            UpdateMapPaint();
            
            // Paso 3: Crear pasillos desde el living room
            // Una vez creado el living room, se crean un pasillo desde el living room a la salida mas lejana
            if (GetFreeM2() > 10 * 5) {
                RayCastResult pasilloPrincipalRaycast = Raycast(_livingRoom, false, new[] { 1 }, new[] { -1, 0 });
                if (pasilloPrincipalRaycast == null)
                    return false;
                
                Room mainHallway = CreateHallway(_livingRoom, pasilloPrincipalRaycast.Side, 5, true);
                if (mainHallway == null) {
                    return false;
                }
                
                mainHallway.Texture = new Texture("Ground",Color.Brown);
                mainHallway.Id = 3;
                TryToPlaceRoom(mainHallway, true);
                hallways.Add(mainHallway);
                hallwaysEnds.Add(mainHallway, mainHallway.points.Last());
            }
            
            UpdateMapPaint();

   
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
            
            
            UpdateMapPaint();
            // Big fill rooms pass
            // Si quedan espacios libres, se crean habitaciones
            while (GetBubbles().Count >= 1) {
                Room b = new Room(GetBubbles()[0]);
                if (b.points.Count < 4) {
                    if (TryToPlaceRoom(b))
                        placedRooms.Add(b);
                    continue;
                }
                
                List<Room> bRooms = b.Grow(Random.Next(1, 1), this);
            
                foreach (var sRoom in bRooms) {
                    sRoom.Text = "sRoom";
                    if (TryToPlaceRoom(sRoom))
                        placedRooms.Add(sRoom);
                }
                
            }

            UpdateMapPaint();

            // Hallway maze pass.
            // Ahora mismo esta to.do lleno, pero, puede que hayan habitaciones muy grandes
            // El hallway maze pass intenta dividir las habitaciones grandes en 2, poniento un pasillo en el medio
            foreach (var placedRoom in placedRooms.ToList()) {
                if ((placedRoom.Height > 6 || placedRoom.Width > 6) &&
                    placedRoom.points.Count >= _livingRoom.points.Count) {
                    placedRooms.Remove(placedRoom);
                    Destroy(placedRoom);
                }
            }
            
            UpdateMapPaint();
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
                        // Room point = new Room(1, 1, middle.Item1, middle.Item2, false, new Texture("Ground",Color.Red));
                        // point.Id = 1;
                        // TryToPlaceRoom(point);
                        
                        
                        RayCastResult raycast = Raycast(middle, true, new[] { 1 }, new[] { 3 });
                        if (raycast == null)
                            continue;
                        
                        if (raycast.Distance == 1)
                            continue;
                            
                        if (furthest == null || raycast.Distance > furthest.Distance) {
                            furthest = raycast;
                        }
                    }
                    
                    if (furthest != null) {
                        Room dividerHallway = furthest.GetAsRoom(true, false);
                        if (dividerHallway.points.Count == 0)
                            continue;
                    
                        dividerHallway.Texture = new Texture("Ground",Color.Brown);
                        dividerHallway.Id = 3;
                        dividerHallway.Text = "ramificacion";
                        TryToPlaceRoom(dividerHallway, true);
                        hasChanged = true;
                        bubbles2.Remove(bubble);
                        hallways.Add(dividerHallway);
                        hallwaysEnds.Add(dividerHallway, dividerHallway.points[0]);
                    }
                }
            }
            
            UpdateMapPaint();
            // Una vez creado el pasillo, se crean habitaciones en los espacios libres
            while (GetBubbles().Count >= 1) {
                Room b = new Room(GetBubbles()[0]);
                b.Text = "b";

                if (b.points.Count >= _livingRoom.points.Count) {
                    
                    int slice = Random.Next(2, 3);
                    int roomSize = b.Width + b.Height / slice;
                    
                    List<Room> bRooms = b.Grow(Random.Next(2, 3), this, roomSize);
                    foreach (var sRoom in bRooms) {
                        sRoom.Text = "xRoom";
                        if (TryToPlaceRoom(sRoom))
                            placedRooms.Add(sRoom);
                    }
                }
                else {                        
                    if (TryToPlaceRoom(b))
                        placedRooms.Add(b);
                }
            }

            if (GetFreeM2() != 0) {
                return false;
            }
            
            
            UpdateMapPaint();
            List<HouseRoomAssigner.RoomInfo> alreadyPlaced = new List<HouseRoomAssigner.RoomInfo>();
            alreadyPlaced.Add( new HouseRoomAssigner.RoomInfo() {
                RoomType = RoomType.LivingRoom,
                Room = _livingRoom,
                IsAlreadyPlaced = true
            });
            if (hall != null)
                alreadyPlaced.Add( new HouseRoomAssigner.RoomInfo() {
                    RoomType = RoomType.Hall,
                    Room = hall,
                    IsAlreadyPlaced = true
                });
            foreach (var hallway in hallways) {
                alreadyPlaced.Add( new HouseRoomAssigner.RoomInfo() {
                    RoomType = RoomType.Hallway,
                    Room = hallway,
                    IsAlreadyPlaced = true,
                    PreferredDoorPosition = hallwaysEnds[hallway]
                });
            }

            HouseRoomAssigner houseRoomAssigner = new HouseRoomAssigner(this);
            List<HouseRoomAssigner.RoomInfo> succedRooms = houseRoomAssigner.Assign(placedRooms, alreadyPlaced);
            
            if (succedRooms == null) {
                return false;
            }
            
            foreach (var placedRoom in placedRooms) {
                Destroy(placedRoom);
            }
            
            foreach (var roomInfo in succedRooms) {
                roomInfo.Room.Text = roomInfo.RoomType.ToString();
                TryToPlaceRoom(roomInfo.Room, true);
            }

            UpdateMapPaint();
            
            Dictionary<(Object, Object), WallInfo> wallInfos = new Dictionary<(Object, Object), WallInfo>();
            
            foreach (var roomInfo in succedRooms) {
                List<Side> sides =  new List<Side>(){Side.Top, Side.Right, Side.Bottom, Side.Left};
                foreach (var side in sides) {
                    foreach (var point in roomInfo.Room.GetPoinsOfSide(side)) {
                        Room room1 = roomInfo.Room;
                        Room room2 = GetRoom(point.Item1 + side.GetX(), point.Item2 + side.GetY());

                        
                        (int, int) wallPoint = (point.Item1 * 2 + 1 + side.GetX(), point.Item2 * 2 + 1 + side.GetY());
                        object secondKey;
                        if (room2 != null)
                            secondKey = room2;
                        else
                            secondKey = side;
                        
                        if (room1 == room2)
                            continue;
                        
                        if (wallInfos.ContainsKey((room1, secondKey))) {
                            if (wallInfos[(room1, secondKey)].Points.Contains(wallPoint))
                                continue;
                            
                            wallInfos[(room1, secondKey)].Points.Add(wallPoint);
                            continue;
                        }
                        if (wallInfos.ContainsKey((secondKey, room1))) {
                            if (wallInfos[(secondKey, room1)].Points.Contains(wallPoint))
                                continue;
                            
                            wallInfos[(secondKey, room1)].Points.Add(wallPoint);
                            continue;
                        }
                        

                        WallInfo wallInfo = new WallInfo();
                        wallInfo.Parent = roomInfo;
                        wallInfo.Horizontal = side == Side.Top || side == Side.Bottom;
                        wallInfo.Child = null;
                        wallInfo.Points.Add(wallPoint);
                        if (room2 != null) 
                            wallInfo.Child = succedRooms.Find(r => r.Room == room2);
                        
                        wallInfos.Add((room1, secondKey), wallInfo);
                    }
                }
            }
            
            UpdateMapPaint();

            foreach (var keyValuePair in wallInfos) {
                WallInfo wallInfo = keyValuePair.Value;
                
                Room room1 = wallInfo.Parent.Room;
                Room room2 = wallInfo.Child?.Room;
                
                wallInfo.MakeWallsAndDoors(_map, this);
            }
            
            UpdateMapPaint();

            // _map.PrintWalls();
            return true;
        }
        
        
        # region Rooms
        public bool AddRoomRandomly(Room room, bool force = false) {
            // Random Approach
            int attempts = 10;
            for (int i = 0; i < attempts; i++) {
                int x = Random.Next(_tileGrid.GetLength(0));
                int y = Random.Next(_tileGrid.GetLength(1));
                if (_tileGrid[x, y] == 1) {
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
                _tileGrid[dx, dy] = room.Id;
                _tileRooms[dx, dy] = room;
            }
            
            return true;
        }

        int _margin = 2;
        public void UpdateMapPaint() {
            // Locked = true;
            // while (Locked) {
            //     Thread.Sleep(10);    
            // }
            
            for (int x = 0; x < _tileGrid.GetLength(0); x++) {
                for (int y = 0; y < _tileGrid.GetLength(1); y++) {
                    Room room = GetRoom(x, y);
                    if (room != null) {
                        if (_tileGrid[x, y] >= 2) {
                            if (new Point2D((x, y)).DistanceTo(new Point2D(((int, int)) room.GetCenter())) < 1) {
                                _map.Paint(room.Texture, x, y, room.Text);
                            }
                            else {
                                _map.Paint(room.Texture, x, y);
                            }
                        }
                        else {
                            _map.Paint(new Texture("Grass"), x, y);
                        }
                    }
                    else {
                        if (_tileGrid[x, y] == -1 || _tileGrid[x, y] == 0) {
                            _map.Paint(new Texture("Grass", Color.Green), x, y);
                        }
                        else {
                            _map.Paint(new Texture("Empty", Color.Black), x, y);
                        }
                    }
                }
            }
        }

        public static bool Locked = false;


        public bool CanPlaceRoom(Room room) {
            foreach (var (dx, dy) in room.points) {
                try {
                    if (_tileGrid[dx, dy] != 1) {
                        return false;
                    }
                }
                catch (Exception) {
                    return false;
                }
            }
            return true;
        }

        public void Destroy(Room room)
        {
            foreach (var valueTuple in room.points) {
                _tileGrid[valueTuple.Item1, valueTuple.Item2] = 1;
                _tileRooms[valueTuple.Item1, valueTuple.Item2] = null;
            }
        }
        
        #endregion
        
        #region Holes
        
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
        public List<List<(int, int)>> GetBubbles() {
            List<List<(int, int)>> bubbles = new List<List<(int, int)>>();
            int[,] tiles = this._tileGrid.Clone() as int[,];
            
            if (tiles == null) {
                return bubbles;
            }
            
            // use method GetFreeHoles to get the bubbles (all different free holes)
            for (int x = 0; x < _tileGrid.GetLength(0); x++) {
                for (int y = 0; y < _tileGrid.GetLength(1); y++) {
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
            
                Room hallway = new Room(1, 1, x, y, false, new Texture("Ground", Color.Black));
                
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
                if (GetTile(roomTip.Extend(pSide1)) == 0 || GetTile(roomTip.Extend(pSide2)) == 0) 
                    wallPoints = (int) (wallPoints * .75f);

                if (TryToPlaceRoom(hallway)) {
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
                    if (bestWallPoints == wallPoints && Random.Next(2) == 0)
                            continue;
                    
                    best = hallway;
                    bestWallPoints = wallPoints;
                }
            }

            if (best != null) {
                Point2D roomTip = new Point2D(best.GetPoinsOfSide(facingSide)[0]);
                Side pSide1 = facingSide.GetPerpendicularSide(false);
                Side pSide2 = facingSide.GetPerpendicularSide(true);

                if (GetTile(roomTip.Extend(pSide1)) == 0 && GetTile(roomTip.Extend(pSide2)) == 0 && GetTile(roomTip.Extend(facingSide)) == 0) {
                    best.UnExtend(facingSide);
                    
                    // todo mejorar esto con matematica
                    try {
                        _tileGrid[roomTip.Extend(facingSide).X, roomTip.Extend(facingSide).Y] = 0;
                    }catch (Exception) {
                        
                    }

                }
            }
            
            return best;
        }
        
        #endregion
        public Room GetRoom(int x, int y) {
            return _tileRooms[x, y];
        }
    }
    public static void Generate(Map map, int seed) {
        Random = new Random(seed);
        Console.Out.WriteLine("seed = {0}", seed);

        int margin = 2;
        
        int random100 = Random.Next(100);
        
        int sx = map.x - margin * 2;
        int sy = map.y - margin * 2;

        PolygonGenerator.TileInfo[,] tiles;

        if (random100 < 33) {
            tiles = PolygonGenerator.CreatePolygon(sx, sy, 0.2);
        }
        else {
            tiles = PolygonGenerator.CreatePolygon(sx, sy, 0);
        }
        
        HouseBuilder houseBuilder = new HouseBuilder(map, tiles, margin);
        if (!houseBuilder.BuildHouse()) {
            Console.Out.WriteLine("No se pudo generar la casa");
            map.Generate(Random.Next());
            return;
        }
    }
    
    
}