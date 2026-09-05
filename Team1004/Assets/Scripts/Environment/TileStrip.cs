using UnityEngine;

namespace Game.Environment
{
    public static class TileStrip
    {
        public static float TotalWidth(float tileWidth, int tileCount)
        {
            if (tileWidth <= 0f || tileCount <= 0)
                return 0f;

            return tileWidth * tileCount;
        }

        public static float OriginX(float tileWidth, int tileCount)
        {
            return TotalWidth(tileWidth, tileCount) * -0.5f;
        }

        public static float Wrap(float value, float totalWidth)
        {
            if (totalWidth <= 0f)
                return 0f;

            if (value >= 0f && value < totalWidth)
                return value;

            var wrapped = value - Mathf.Floor(value / totalWidth) * totalWidth;

            if (wrapped < 0f)
                wrapped += totalWidth;

            return wrapped >= totalWidth ? 0f : wrapped;
        }

        public static float Advance(float offset, float delta, float totalWidth)
        {
            return Wrap(offset + delta, totalWidth);
        }

        public static float MinimumTotalWidth(float tileWidth, float viewWidth)
        {
            return Mathf.Max(0f, viewWidth) + Mathf.Max(0f, tileWidth) * 2f;
        }

        public static int MinimumTileCount(float tileWidth, float viewWidth)
        {
            if (tileWidth <= 0f)
                return 0;

            return Mathf.Max(2, Mathf.CeilToInt(MinimumTotalWidth(tileWidth, viewWidth) / tileWidth));
        }

        public static bool CoversView(float tileWidth, int tileCount, float viewWidth)
        {
            return TotalWidth(tileWidth, tileCount) >= MinimumTotalWidth(tileWidth, viewWidth) - 1e-4f;
        }

        public static float TileX(float tileWidth, int tileCount, float offset, int index)
        {
            var total = TotalWidth(tileWidth, tileCount);

            if (total <= 0f)
                return 0f;

            var slot = Wrap(index * tileWidth - offset, total);
            return OriginX(tileWidth, tileCount) + slot + tileWidth * 0.5f;
        }
    }
}
