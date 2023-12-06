using System;
using System.Collections.Generic;



public class PolygonGenerator
{
    private static bool IsCorner(int i, int j, TileInfo[,] tiles) {
        // Condiciones de límites
        bool isAboveFilled = i > 0 ? tiles[i - 1, j].IsFilled : false;
        bool isBelowFilled = i < tiles.GetLength(0) - 1 ? tiles[i + 1, j].IsFilled : false;
        bool isLeftFilled = j > 0 ? tiles[i, j - 1].IsFilled : false;
        bool isRightFilled = j < tiles.GetLength(1) - 1 ? tiles[i, j + 1].IsFilled : false;

        // Verifica si el tile actual es una esquina en función de sus vecinos.
        // Esto es si tiene 2 vecinos juntos (L) y 2 vecinos opuestos Vacios
        // Ejemplo:
        // 0 0 0 0 0
        // 0 2 1 2 0
        // 0 1 1 1 0
        // 0 2 1 2 0
        // 0 0 0 0 0
        // los 2 son esquinas porque tiene dos 1 y dos 0.
        return tiles[i, j].IsFilled && 
               ((isAboveFilled && isLeftFilled && !isBelowFilled && !isRightFilled) ||
                (isAboveFilled && isRightFilled && !isBelowFilled && !isLeftFilled) ||
                (isBelowFilled && isLeftFilled && !isAboveFilled && !isRightFilled) ||
                (isBelowFilled && isRightFilled && !isAboveFilled && !isLeftFilled));
    }

    private static List<(int, int)> GetCorners(TileInfo[,] tiles)
    {
        var corners = new List<(int, int)>();
    
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                if (IsCorner(i, j, tiles))
                {
                    corners.Add((i, j));
                }
            }
        }
    
        return corners;
    }
    
    public class TileInfo {
        public bool IsFilled { get; set; }
    }

    public static TileInfo[,] CreatePolygon(int width, int height, double percentToRemove)
    {
        // Crear la matriz de Tiles
        var tiles = new TileInfo[width, height];

        // Rellena la matriz de tiles
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                tiles[i, j] = new TileInfo { IsFilled = true };
            }
        }

        // Calcula el número total de tiles a eliminar
        int totalTiles = width * height;
        int tilesToRemove = (int)(totalTiles * percentToRemove);
        
        // Eliminar los tiles necesarios para cumplir con el porcentaje
        while (tilesToRemove > 5)
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

            // Encontrar el punto de inicio en una esquina
            var startPoint = GetRandomCorner(tiles);
            // tiles[startPoint.Item1, startPoint.Item2].IsFilled = false;
            // break;
            // }
            int startX = startPoint.Item1;
            int startY = startPoint.Item2;
        
            // Asegurar que la zona vacía cabe en el espacio restante
            vacateWidth = Math.Min(vacateWidth, tiles.GetLength(0) - startX);
            vacateHeight = Math.Min(vacateHeight, tiles.GetLength(1) - startY);
                
            for (int i = 0; i < vacateWidth; i++)
            {
                for (int j = 0; j < vacateHeight; j++)
                {
                    if (tiles[startX + i, startY + j].IsFilled)
                    {
                        tiles[startX + i, startY + j].IsFilled = false;
                        tilesToRemove--;
                            
                        if (tilesToRemove <= 0)
                        {
                            break;
                        }  
                    }
                }
                 
                if (tilesToRemove <= 0)
                {
                    break;
                }
            }
        }
        
        return tiles;
    }

    private static (int, int) GetRandomCorner(TileInfo[,] tiles)
    {
        var corners = GetCorners(tiles);
        
        var index = HouseGenerator.Random.Next(corners.Count);
        
        return corners[index];
    }
}