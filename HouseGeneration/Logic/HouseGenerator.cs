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
    }
    
    public class HouseBuilder {
        
        private int[,] tileGrid;
        // 0 = outside, 1 = free, 2 = occupied, 3 = occupied_but_accessible
        private Room[,] tileRooms;
        private Room livingRoom;
        private List<Room> rooms = new List<Room>();

        public HouseBuilder(TileInfo[,] tileGrid) {
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

        public void BuildHouse()
        {
            // Paso 1: Crear Living room
            livingRoom = CreateLivingRoom();
            rooms.Add(livingRoom);
            
            // Paso 2: Crear pasillos desde el living room
            // var hallways = CreateHallwaysFromRoom(livingRoom);
            //
            // // Paso 3: Crear habitaciones desde los pasillos
            // foreach (var hallway in hallways)
            // {
            //     var roomsFromHallway = CreateRoomsFromHallway(hallway);
            //     rooms.AddRange(roomsFromHallway);
            // }
            // // Paso 4: Dividir habitaciones si es posible
            // foreach(var room in rooms){
            //     List<Room> dividedRooms = DivideRoomIfPossible(room);
            //     // Reemplazar la habitación original con las habitaciones divididas
            //     rooms.Remove(room);
            //     rooms.AddRange(dividedRooms);
            // }
        }

        private Room CreateLivingRoom()
        {
            // Tu lógica para crear el Living room
            return new Room(1, 1);
        }
        
        private void AddRoom(Room room) {
            rooms.Add(room);
            for (int x = 0; x < room.Width; x++) {
                for (int y = 0; y < room.Height; y++) {
                    tileGrid[x, y] = 2;
                    tileRooms[x, y] = room;
                }
            }
        }

        // private List<Hallway> CreateHallwaysFromRoom(Room room)
        // {
        //     // Tu lógica para crear pasillos desde la habitación
        // }
    
        public Room getRoom(int x, int y)
        {
            // if (x < 0 || x >= map.GetLength(0) || y < 0 || y >= map.GetLength(1)) {
            // return null;
            // }
            
            // return map[x, y];
            if (tileGrid[x, y] == 0) {
                return null;
            }
            return new Room(1,1 , false, Color.White);
        }
    }
    public static void Generate(Map map) {
        Side enterSide = Side.Top;
        int Margin = 2;
        
        int sx = map.x - Margin * 2;
        int sy = map.y - Margin * 2;

        TileInfo[,] tiles = CreatePolygon(sx, sy, 0.2);
        
        HouseBuilder houseBuilder = new HouseBuilder(tiles);
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
    
    public class TileInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsFilled { get; set; }
    }

    public static TileInfo[,] CreatePolygon(int width, int height, double percentToRemove)
    {
        // Crear la matriz de Tiles
        var tiles = new TileInfo[width, height];

        // Rellena la matriz de Tiles
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                tiles[i, j] = new TileInfo { Width = i, Height = j, IsFilled = true };
            }
        }

        // Calcula el número total de Tiles a eliminar
        int totalTiles = width * height;
        int tilesToRemove = (int)(totalTiles * percentToRemove);

        Random rand = new Random();
        
        // Eliminar los Tiles necesarios para cumplir con el porcentaje 
        while (tilesToRemove > 0)
        {
            int vacateWidth, vacateHeight;

            if (tilesToRemove >= 20)
            {
                vacateWidth = 5;
                vacateHeight = 2;
            }
            else if (tilesToRemove >= 4)
            {
                vacateWidth = 4;
                vacateHeight = 1;
            }
            else
            {
                vacateWidth = 1;
                vacateHeight = 1;
            }

            int startX = rand.Next(width - vacateWidth + 1);
            int startY = rand.Next(height - vacateHeight + 1);
            
            for (int i = 0; i < vacateWidth; i++)
            {
                for (int j = 0; j < vacateHeight; j++)
                {
                    if (tiles[startX + i, startY + j].IsFilled)
                    {
                        tiles[startX + i, startY + j].IsFilled = false;
                        tilesToRemove--;
                        
                        if(tilesToRemove <= 0) break;
                    }
                }
                if(tilesToRemove <= 0) break;
            }
        }
        
        return tiles;
    }
}