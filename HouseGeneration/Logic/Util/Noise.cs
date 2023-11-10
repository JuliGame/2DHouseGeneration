using System;
using SharpNoise;
using SharpNoise.Modules;

namespace HouseGeneration.Logic.Util;

public class Noise {
    
    private static Random random = new Random();
    public static int[,] ApplyCellularNoise(int[,] input, int numIterations) {
        int width = input.GetLength(0);
        int height = input.GetLength(1);

        int[,] output = new int[width, height];
        int cellSize = 3; // Tamaño de la celda 3x3

        for (int iteration = 0; iteration < numIterations; iteration++)
        {
            for (int y = 0; y < height; y += cellSize)
            {
                for (int x = 0; x < width; x += cellSize)
                {
                    int cellValue = input[x, y];

                    // Aplicar ruido celular a la celda actual
                    int noise = CalculateCellularNoise(x, y);

                    // Desplazar la celda según el ruido Perlin
                    int offsetX = (int)(noise * 2); // Ajusta el valor según tus necesidades
                    int offsetY = (int)(noise * 2); // Ajusta el valor según tus necesidades

                    for (int dy = 0; dy < cellSize; dy++)
                    {
                        for (int dx = 0; dx < cellSize; dx++)
                        {
                            int newX = x + dx + offsetX;
                            int newY = y + dy + offsetY;

                            if (newX >= 0 && newX < width && newY >= 0 && newY < height)
                            {
                                output[newX, newY] = cellValue;
                            }
                        }
                    }
                }
            }
        }

        return output;
    }
    public static int[,] CreateUniqueMatrix(int x, int width, int height) {
        int gridWidth = width / x + 1;
        int gridHeight = height / x + 1;
        int[,] grid = new int[height, width];

        int uniqueValue = 0;
        for (int row = 0; row < gridHeight; row++)
        {
            for (int col = 0; col < gridWidth; col++)
            {
                for (int i = 0; i < x; i++)
                {
                    for (int j = 0; j < x; j++)
                    {
                        if (row * x + i >= height || col * x + j >= width) {
                            continue;
                        }
                        
                        grid[row * x + i, col * x + j] = uniqueValue;
                    }
                }
                uniqueValue++;
            }
        }

        return grid;
    }
    private static int CalculateCellularNoise(int x, int y)
    {
        // Crear el modelo de ruido celular
        Cell cell = new Cell();

        // Configurar los parámetros del modelo de ruido celular
        cell.Frequency = .10;  // Ajusta la frecuencia según tus necesidades
        cell.Seed = random.Next();  // Puedes cambiar la semilla si lo deseas

        // Calcular el valor de ruido en las coordenadas (x, y)
        double noiseValue = cell.GetValue(x, y, 0);

        // Mapea el valor de ruido a un rango entre -1 y 1
        int mappedValue = (int)(noiseValue * 2);

        return mappedValue;
    }
    public static void PrintMatrix(int[,] matrix) {
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Console.Write(matrix[x, y] + " ");
            }
            Console.WriteLine();
        }
    }
}
