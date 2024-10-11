using System;

namespace Shared.ProceduralGeneration.Util
{
    public static class ClampUtility
    {
        public static T ClampValue<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
    }
}