using UnityEngine;

namespace Game.Particles
{
    public static class ParticleMath
    {
        public const float MinFade = 0.0001f;

        public static float AlphaOverLife(float life01, float fadeIn, float fadeOut)
        {
            if (life01 <= 0f || life01 >= 1f)
                return 0f;

            var rise = fadeIn <= MinFade ? 1f : life01 / fadeIn;
            var fall = fadeOut <= MinFade ? 1f : (1f - life01) / fadeOut;

            return Mathf.Clamp01(Mathf.Min(rise, fall));
        }

        public static float SizeOverLife(float life01, float endScale)
        {
            return Mathf.LerpUnclamped(1f, endScale, Mathf.Clamp01(life01));
        }

        public static float Drift(float time, float phase, float amplitude, float frequency)
        {
            if (amplitude == 0f || frequency == 0f)
                return 0f;

            return amplitude * Mathf.Sin((time * frequency + phase) * Mathf.PI * 2f);
        }

        public static Vector2 Direction(float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        public static int TakeSpawnCount(ref float accumulator, float amount, float rate, int max)
        {
            if (rate <= 0f || amount <= 0f || max <= 0)
                return 0;

            accumulator += amount * rate;

            if (accumulator < 1f)
                return 0;

            var count = Mathf.FloorToInt(accumulator);

            if (count > max)
            {
                accumulator = 0f;
                return max;
            }

            accumulator -= count;
            return count;
        }

        public static bool IsOutsideBounds(Vector2 point, Vector2 center, Vector2 size, float margin)
        {
            var halfX = Mathf.Abs(size.x) * 0.5f + margin;
            var halfY = Mathf.Abs(size.y) * 0.5f + margin;

            return point.x < center.x - halfX || point.x > center.x + halfX ||
                   point.y < center.y - halfY || point.y > center.y + halfY;
        }

        public static float LocalScaleFor(float worldWidth, float nativeWidth)
        {
            if (nativeWidth <= MinFade)
                return worldWidth;

            return worldWidth / nativeWidth;
        }
    }
}
