using UnityEngine;

namespace Game.Water
{
    public static class RangeIntExtensions
    {
        public static RangeInt Intersect(this RangeInt a, RangeInt b)
        {
            int start = Mathf.Max(a.start, b.start);
            int end = Mathf.Min(a.end, b.end);
            return start >= end ? new RangeInt(0, 0) : new RangeInt(start, end - start);
        }
    }
}
