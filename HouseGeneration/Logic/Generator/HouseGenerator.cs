using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace HouseGeneration.Logic;

public class HouseGenerator
{
    public static Random random = new Random();

    public class Room
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public bool MustBeAtEdge { get; set; }
        public Color Color;
        public Room(int width, int height, bool mustBeAtEdge = false, Color color = default)
        {
            Width = width;
            Height = height;
            MustBeAtEdge = mustBeAtEdge;
            
            if (color != default) {
                Color = color;
            }
            else {
                Color = Color.FromNonPremultiplied(random.Next(255), random.Next(255), random.Next(255), 255);
            }
        }
        
        public Room(int area, float noSquareness,  bool mustBeAtEdge = false, Color color = default) {
            if(noSquareness <= 0)
                noSquareness = 0.01f;
            else if(noSquareness >= 1)
                noSquareness = 0.99f;
            
            if(noSquareness == 1) {
                // La mayor área cuadrada posible
                Width = Height = (int)Math.Sqrt(area);
            }
            else if(noSquareness == 0) {
                // Un rectángulo de 1 de altura
                Width = area;
                Height = 1;
            }
            else {
                // Un rectángulo de una altura proporcionalmente menor a la de un cuadrado perfecto
                Height = (int)Math.Sqrt(area) - (int)(Math.Sqrt(area) * noSquareness);
                if (Height == 0) {
                    Height = 1;
                }
                Width = area / Height;
            }
            
            if (random.NextDouble() < 0.5) {
                (Width, Height) = (Height, Width);
            }

            
            MustBeAtEdge = mustBeAtEdge;
            
            if (color != default) {
                Color = color;
            }
            else {
                Color = Color.FromNonPremultiplied(random.Next(255), random.Next(255), random.Next(255), 255);
            }
        }

        public List<(int, int)> points = null;
        public Room(List<(int, int)> points, Color color = default) {
            this.points = points;
            
            if (color != default) {
                Color = color;
            }
            else {
                Color = Color.FromNonPremultiplied(random.Next(255), random.Next(255), random.Next(255), 255);
            }
        }

        public void extend(Side facingSide)
        {
            switch (facingSide)
            {
                case Side.Top:
                    Height++;
                    Y--;
                    break;
                case Side.Bottom:
                    Height++;
                    break;
                case Side.Left:
                    Width++;
                    X--;
                    break;
                case Side.Right:
                    Width++;
                    break;
            }
        }
        
        public void unextend(Side facingSide)
        {
            switch (facingSide)
            {
                case Side.Top:
                    Height--;
                    Y++;
                    break;
                case Side.Bottom:
                    Height--;
                    break;
                case Side.Left:
                    Width--;
                    X++;
                    break;
                case Side.Right:
                    Width--;
                    break;
            }
        }

        public bool isNextTo(int x, int y) {
            if (x == X + Width && y >= Y && y < Y + Height) {
                return true;
            }
            else if (x == X - 1 && y >= Y && y < Y + Height) {
                return true;
            }
            else if (y == Y + Height && x >= X && x < X + Width) {
                return true;
            }
            else if (y == Y - 1 && x >= X && x < X + Width) {
                return true;
            }

            return false;
        }
    }
    
    public class HouseBuilder {
        
        private int[,] tileGrid;
        // 0 = outside, 1 = free, 2 = occupied, 3 = occupied_but_accessible
        private Room[,] tileRooms;
        private Room livingRoom;
        private List<Room> rooms = new List<Room>();

        public HouseBuilder(PolygonGenerator.TileInfo[,] tileGrid) {
            this.tileGrid = new int[tileGrid.GetLength(0), tileGrid.GetLength(1)];
            for (int x = 0; x < tileGrid.GetLength(0); x++) {
                for (int y = 0; y < tileGrid.GetLength(1); y++) {
                    this.tileGrid[x, y] = tileGrid[x, y].IsFilled ? 1 : 0;
                }
            }
            
            tileRooms = new Room[tileGrid.GetLength(0), tileGrid.GetLength(1)];
        }
        
        public int getFreeM2() {
            int freeM2 = 0;
            foreach (var isFree in tileGrid) {
                freeM2 += isFree == 1 ? 1 : 0;
            }
            return freeM2;
        }

        public bool BuildHouse()
        {
            // Paso 1: Crear Living room
            float size = 0.3f;
            livingRoom = GetLivingRoom(size);
            while (!AddRoomRandomly(livingRoom)) {
                size -= 0.01f;
                if (size < 0.1f) {
                    Console.Out.WriteLine("1");
                    return false;
                }
                
                livingRoom = GetLivingRoom(size);
            }

            // Paso 2: Crear pasillos desde el living room
            int cachos = 2 + random.Next(3);
            if (!CreateHallwayFromRoom(livingRoom, cachos, 0, new List<Room>())) {
                Console.Out.WriteLine("2");
                return false;
            }

            // // Paso 3: Crear habitaciones desde los pasillos
            List<List<(int, int)>> bubbles = GetBubbles();
            foreach (var bubble in bubbles) {
                Room room = new Room(bubble);
                TryToPlaceRoomAt(room);
            }
            
            // // Paso 4: Dividir habitaciones si es posible
            // foreach(var room in rooms){
            //     List<Room> dividedRooms = DivideRoomIfPossible(room);
            //     // Reemplazar la habitación original con las habitaciones divididas
            //     rooms.Remove(room);
            //     rooms.AddRange(dividedRooms);
            // }
            
            return true;
        }

        private Room GetLivingRoom(float size) {
            int freeM2 = getFreeM2();
            int roomM2 = (int) (freeM2 * size);
            float noSquareness = random.Next(30) / 100f;
            return new Room(roomM2, noSquareness, false, Color.Red);
        }
        # region Rooms
        public bool AddRoomRandomly(Room room, bool force = false) {
            // Random Approach
            int attempts = 10;
            for (int i = 0; i < attempts; i++) {
                int x = random.Next(tileGrid.GetLength(0));
                int y = random.Next(tileGrid.GetLength(1));
                if (tileGrid[x, y] == 1) {
                    if (!TryToPlaceRoomAt(room, x, y))
                        continue;
                    
                    return true;
                }
            }
            
            // Brute Force Approach
            for (int x = 0; x < room.Width; x++) {
                for (int y = 0; y < room.Height; y++) {
                    if (!TryToPlaceRoomAt(room, x, y))
                        continue;
                    
                    return true;
                }
            }
            
            if (force) {
                
            }

            return false;
        }

        public bool TryToPlaceRoomAt(Room room, int x, int y) {
            if (!CanPlaceRoomAt(room, x, y))
                return false;
            
            for (int dx = 0; dx < room.Width; dx++)
            {
                for (int dy = 0; dy < room.Height; dy++)
                {
                    tileGrid[x + dx, y + dy] = 2;
                    tileRooms[x + dx, y + dy] = room;
                }
            }

            room.X = x;
            room.Y = y;
            return true;
        }
        
        public bool CanPlaceRoomAt(Room room, int x, int y) {
            for (int dx = 0; dx < room.Width; dx++) {
                for (int dy = 0; dy < room.Height; dy++) {
                
                    try {
                        if (tileGrid[x + dx, y + dy] != 1) {
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public bool TryToPlaceRoomAt(Room room) {
            if (!CanPlaceRoomAt(room))
                return false;
            
            foreach (var (dx, dy) in room.points) {
                tileGrid[dx, dy] = 2;
                tileRooms[dx, dy] = room;
            }
            
            return true;
        }

        
        public bool CanPlaceRoomAt(Room room) {
            foreach (var (dx, dy) in room.points)
            {
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

        public void Destroy(Room room) {
            for (int x = 0; x < room.Width; x++) {
                for (int y = 0; y < room.Height; y++) {
                    tileGrid[x + room.X, y + room.Y] = 1;
                    tileRooms[x + room.X, y + room.Y] = null;
                }
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
        private bool CreateHallway(Room room, List<Room> hallways, Side facingSide, int depth = 0) {
            if (depth > 30)
                return false;
            
            // select a random square from the side of the room
            int x = room.X;
            int y = room.Y;
                
            if (facingSide == Side.Top) {
                x += random.Next(room.Width);
                y -= 1;
            }
            else if (facingSide == Side.Bottom) {
                x += random.Next(room.Width);
                y += room.Height;
            }
            else if (facingSide == Side.Left) {
                x -= 1;
                y += random.Next(room.Height);
            }
            else if (facingSide == Side.Right) {
                x += room.Width;
                y += random.Next(room.Height);
            }
            
            // Check if hallway is next to another hallway
            foreach (var hallway1 in hallways) {
                if (hallway1.isNextTo(x, y)) {
                    return CreateHallway(room, hallways, facingSide, depth + 1);
                }
            }
            
            Room hallway = new Room(1, 1, false, Color.Chocolate);
            while (CanPlaceRoomAt(hallway, x + hallway.X, y + hallway.Y)) {
                hallway.extend(facingSide);
            }
            hallway.unextend(facingSide);
            if (!TryToPlaceRoomAt(hallway, x + hallway.X, y + hallway.Y)) {
                return CreateHallway(room, hallways, facingSide, depth + 1);
            }
            
            hallways.Add(hallway);
            return true;
        }

        private bool CreateHallwayFromRoom(Room room, int ammount, int depth, List<Room> hallways) {
            if (depth > 10) {
                Console.Out.WriteLine("2.1");
                return false;
            }
            
            // Si hay
            
            List<Side> sides = new List<Side>();
            sides.Add(Side.Top);
            sides.Add(Side.Bottom);
            sides.Add(Side.Left);
            sides.Add(Side.Right);
            for (int i = 0; i < ammount; i++) {
                Side facingSide;
                if (sides.Count == 0)
                    facingSide = (Side)random.Next(4);
                else
                    facingSide = sides[random.Next(sides.Count)];
                
                sides.Remove(facingSide);
                
                if (!CreateHallway(room, hallways, facingSide)) {
                    Console.Out.WriteLine("2.2   " + ammount);
                    return true;
                }
            }


            List<int> holes = GetFreeHolesSizes();
            
            bool hastoend = false;
            while (GetFreeHolesSizes().Count < ammount && !hastoend) {
                if (hallways.Count == 0) {
                    Console.Out.WriteLine("2.3");
                    return false;
                }
                
                foreach (var hallway in hallways.ToArray()) {
                    // 30 %
                    if (random.NextDouble() < 0.3) {
                        if (!CreateHallwayFromRoom(hallway, random.NextDouble() < 0.5 ? 1 : 2, depth + 1, hallways))
                            hastoend = true;
                    }
                }
            }
            
            int smallRooms = 0;
            foreach (var freeHolesSiz in holes) {
                if (freeHolesSiz < 6) {
                    smallRooms++;
                }
                if (freeHolesSiz < 2) {
                    smallRooms = 2;
                }
            }

            if (smallRooms > 2 || hastoend) {
                foreach (var hallway in hallways) {
                    Destroy(hallway);
                }
                
                return CreateHallwayFromRoom(room, ammount, depth, hallways);
            }

            return true;
        }
        #endregion
        public Room getRoom(int x, int y)
        {
            // if (x < 0 || x >= map.GetLength(0) || y < 0 || y >= map.GetLength(1)) {
            // return null;
            // }
            
            // return map[x, y];

            if (tileRooms[x, y] == null) {
                if (tileGrid[x, y] == 0) {
                    return null;
                }
                else {
                    return new Room(1, 1, false, Color.Gainsboro);
                }
            }
            return tileRooms[x, y];
        }
    }
    public static void Generate(Map map) {
        Side enterSide = Side.Top;
        int Margin = 2;
        
        int sx = map.x - Margin * 2;
        int sy = map.y - Margin * 2;

        PolygonGenerator.TileInfo[,] tiles = PolygonGenerator.CreatePolygon(sx, sy, 0.2);
        
        HouseBuilder houseBuilder = new HouseBuilder(tiles);
        if (!houseBuilder.BuildHouse()) {
            Console.Out.WriteLine("No se pudo generar la casa");
            map.Generate();
            return;
        }

        Room garage = new Room(4, 4, true);
        Room livingRoom = new Room(4, 4);
        Room kitchen = new Room(3, 3);
        Room bathroom = new Room(2, 2);
        Room bedroom = new Room(2, 3);
        
        // houseBuilder.AddRoom(garage);
        // houseBuilder.AddRoom(livingRoom);
        // houseBuilder.AddRoom(kitchen);
        // houseBuilder.AddRoom(bathroom);
        // houseBuilder.AddRoom(bedroom);
        
        for (int x = 0; x < sx; x++) {
            for (int y = 0; y < sy; y++) {
                Room room = houseBuilder.getRoom(x, y);
                if (room != null) {
                    map.Paint(room.Color, x + Margin, y + Margin);
                }
            }
        }
    }
    
    
}