using UnityEngine;

namespace Game.Environment.Editor
{
    public static class EnvironmentPatterns
    {
        private const float Tau = Mathf.PI * 2f;

        public static Color Sky(float u, float v)
        {
            var top = new Color(0.62f, 0.82f, 0.9f, 1f);
            var bottom = new Color(0.85f, 0.94f, 0.96f, 1f);
            var color = Color.Lerp(bottom, top, v);
            color.a = 1f;
            return color;
        }

        public static Color Far(float u, float v)
        {
            var deep = new Color(0.02f, 0.13f, 0.18f, 1f);
            var shallow = new Color(0.05f, 0.26f, 0.33f, 1f);
            var color = Color.Lerp(deep, shallow, Mathf.SmoothStep(0f, 1f, v));

            var band = 0.5f + 0.5f * Mathf.Sin(Tau * 2f * u);
            var shaft = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(Tau * u + 1.1f), 4f) * v;
            var lift = band * 0.02f + shaft * 0.05f;

            color.r += lift;
            color.g += lift * 1.4f;
            color.b += lift * 1.6f;
            color.a = 1f;
            return color;
        }

        public static Color Mid(float u, float v)
        {
            var wave = Mathf.Sin(Tau * 3f * u + 1.6f * Mathf.Sin(Tau * v));
            var ripple = Mathf.Pow(Mathf.Clamp01(wave), 6f);
            var fade = Mathf.Sin(Mathf.PI * v);
            var alpha = ripple * fade * 0.16f;
            return new Color(0.45f, 0.78f, 0.82f, alpha);
        }

        public static Color Near(float u, float v)
        {
            var ground = new Color(0.04f, 0.11f, 0.14f, 1f);
            var pebble = new Color(0.09f, 0.18f, 0.2f, 1f);

            var crest = 0.52f
                        + 0.06f * Mathf.Sin(Tau * u)
                        + 0.04f * Mathf.Sin(Tau * 3f * u + 0.9f)
                        + 0.02f * Mathf.Sin(Tau * 7f * u + 2.3f);

            if (v > crest)
                return new Color(0f, 0f, 0f, 0f);

            var bump = 0.5f + 0.5f * Mathf.Sin(Tau * 9f * u + 1.7f);
            var depth = Mathf.Clamp01((crest - v) / Mathf.Max(0.01f, crest));
            var color = Color.Lerp(pebble, ground, depth);
            color = Color.Lerp(color, pebble, bump * (1f - depth) * 0.6f);
            color.a = 1f;
            return color;
        }

        public static Color Flow(float u, float v)
        {
            var streak = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(Tau * 2f * u + Mathf.PI * v)), 8f);
            var band = Mathf.Sin(Mathf.PI * v);
            var alpha = streak * band * 0.22f;
            return new Color(0.82f, 0.94f, 0.98f, alpha);
        }

        public static Color Homeland(float u, float v)
        {
            var gravel = new Color(0.16f, 0.15f, 0.13f, 1f);
            var weed = new Color(0.11f, 0.31f, 0.18f, 1f);

            var moundCenter = 0.28f;
            var moundHalf = 0.2f;
            var moundDistance = Mathf.Abs(u - moundCenter) / moundHalf;
            var moundTop = moundDistance < 1f ? 0.34f * Mathf.Cos(moundDistance * Mathf.PI * 0.5f) : 0f;

            if (v < moundTop)
                return gravel;

            for (var i = 0; i < 3; i++)
            {
                var baseX = 0.6f + i * 0.13f;
                var height = 0.62f + i * 0.1f;

                if (v > height)
                    continue;

                var sway = 0.035f * Mathf.Sin(Tau * (v * 0.8f + i * 0.3f));
                var half = Mathf.Lerp(0.022f, 0.008f, v / Mathf.Max(0.01f, height));

                if (Mathf.Abs(u - (baseX + sway)) < half)
                    return weed;
            }

            return new Color(0f, 0f, 0f, 0f);
        }

        public static Color Calm(float u, float v)
        {
            return new Color(0.78f, 0.92f, 0.98f, 0.12f);
        }

        public static Color WaterFill(float u, float v)
        {
            var tint = new Color(0.32f, 0.72f, 0.82f, 0.38f);
            tint.a += 0.06f * Mathf.Sin(Tau * u);
            return tint;
        }
    }
}
