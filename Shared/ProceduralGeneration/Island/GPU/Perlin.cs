using ILGPU.Algorithms;

namespace Shared.ProceduralGeneration.Island
{
    public static class GPUPerlin
    {
        
        // GPU PERLIN NOISE
        public static float PerlinNoise(float x, float y)
        {
            int x0 = (int)XMath.Floor(x);
            int y0 = (int)XMath.Floor(y);
            float x1 = x - x0;
            float y1 = y - y0;

            float u = GPUtils.Fade(x1);
            float v = GPUtils.Fade(y1);

            float a = GPUtils.Grad(GPUtils.Hash(x0, y0), x1, y1);
            float b = GPUtils.Grad(GPUtils.Hash(x0 + 1, y0), x1 - 1, y1);
            float c = GPUtils.Grad(GPUtils.Hash(x0, y0 + 1), x1, y1 - 1);
            float d = GPUtils.Grad(GPUtils.Hash(x0 + 1, y0 + 1), x1 - 1, y1 - 1);

            float result = GPUtils.Lerp(
                GPUtils.Lerp(a, b, u),
                GPUtils.Lerp(c, d, u),
                v);

            return result;
        }
    }
}