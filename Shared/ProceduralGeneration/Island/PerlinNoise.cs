using System;

namespace Shared.ProceduralGeneration.Util
{
    public class PerlinNoise
    {
        private int[] permutation;
        private Random random;

        public PerlinNoise(int seed)
        {
            random = new Random(seed);
            permutation = new int[512];
            for (int i = 0; i < 256; i++)
            {
                permutation[i] = permutation[i + 256] = random.Next(256);
            }
        }

        public double Noise(double x, double y)
        {
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;

            x -= Math.Floor(x);
            y -= Math.Floor(y);

            double u = Fade(x);
            double v = Fade(y);

            int A = permutation[X] + Y;
            int B = permutation[X + 1] + Y;

            return Lerp(v, Lerp(u, Grad(permutation[A], x, y),
                                   Grad(permutation[B], x - 1, y)),
                           Lerp(u, Grad(permutation[A + 1], x, y - 1),
                                   Grad(permutation[B + 1], x - 1, y - 1)));
        }

        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);

        private static double Lerp(double t, double a, double b) => a + t * (b - a);

        private static double Grad(int hash, double x, double y)
        {
            int h = hash & 15;
            double grad = 1 + (h & 7);
            if ((h & 8) != 0) grad = -grad;
            return (((h & 1) != 0) ? x : y) * grad;
        }
    }
}