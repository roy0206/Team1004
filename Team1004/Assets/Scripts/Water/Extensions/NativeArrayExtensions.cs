using Unity.Collections;
using UnityEngine;

namespace Game.Water
{
    public static class NativeArrayExtensions
    {
        public static NativeArray<T> GetSubArray<T>(this NativeArray<T> array, RangeInt range) where T : struct
            => array.GetSubArray(range.start, range.length);
    }
}
