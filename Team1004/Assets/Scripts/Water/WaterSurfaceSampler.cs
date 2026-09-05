using Unity.Collections;
using UnityEngine;

namespace Game.Water
{
    public static class WaterSurfaceSampler
    {
        public static Water Find()
        {
            return Object.FindFirstObjectByType<Water>();
        }

        public static bool TrySampleHeight(Water water, float x, out float height)
        {
            height = 0f;

            if (water == null)
                return false;

            var bounds = water.bounds;
            height = bounds.yMax;

            var settings = WaterSettings.currentSettings;

            if (settings == null || settings.nodePerUnit <= 0f)
                return true;

            if (!WaterSystem.GetPositions(water, out NativeArray<float> positions, out RangeInt range) || range.length < 1)
                return true;

            var node = (x - bounds.xMin) * settings.nodePerUnit;
            var low = Mathf.Clamp(Mathf.FloorToInt(node), range.start, range.end - 1);
            var high = Mathf.Clamp(low + 1, range.start, range.end - 1);
            var blend = Mathf.Clamp01(node - low);

            height = bounds.yMax + Mathf.Lerp(positions[low - range.start], positions[high - range.start], blend);
            return true;
        }
    }
}
