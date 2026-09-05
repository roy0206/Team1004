using UnityEngine;

namespace Game.Ledge.Editor
{
    public static class LedgePatterns
    {
        private const float Tau = Mathf.PI * 2f;

        public static Color Rock(float u, float v)
        {
            var deep = new Color(0.05f, 0.09f, 0.11f, 1f);
            var lit = new Color(0.14f, 0.19f, 0.21f, 1f);
            var color = Color.Lerp(deep, lit, Mathf.SmoothStep(0f, 1f, v));

            var grain =
                Mathf.Sin(Tau * 3f * u + 0.7f) * 0.5f +
                Mathf.Sin(Tau * 7f * u + 2.3f) * 0.3f +
                Mathf.Sin(Tau * 13f * u + 4.1f) * 0.2f;

            var block = Mathf.Pow(Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin(Tau * 2f * u + Mathf.Sin(Tau * v) * 0.8f)), 5f);
            var lift = grain * 0.018f + block * 0.05f * v;

            color.r += lift;
            color.g += lift * 1.15f;
            color.b += lift * 1.25f;
            color.a = 1f;
            return color;
        }

        public static Color RockEdge(float u, float v)
        {
            var color = Rock(u, v);
            var top = Mathf.Pow(Mathf.Clamp01((v - 0.72f) / 0.28f), 1.4f);

            color.r += top * 0.16f;
            color.g += top * 0.22f;
            color.b += top * 0.24f;
            color.a = 1f;
            return color;
        }

        public static Color Hint(float u, float v)
        {
            var x = u - 0.5f;
            var inside = false;

            if (v >= 0.06f && v <= 0.58f && Mathf.Abs(x) <= 0.13f)
                inside = true;

            if (v > 0.58f && v <= 0.94f)
            {
                var half = 0.38f * Mathf.Clamp01((0.94f - v) / 0.36f);

                if (Mathf.Abs(x) <= half)
                    inside = true;
            }

            if (!inside)
                return new Color(0f, 0f, 0f, 0f);

            var glow = 0.75f + 0.25f * Mathf.Clamp01(v);
            return new Color(0.86f * glow, 0.96f * glow, 1f * glow, 0.82f);
        }
    }
}
