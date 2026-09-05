using UnityEngine;

namespace Game.View
{
    public static class CameraEasing
    {
        private const float BackOvershoot = 1.70158f;

        public static float Evaluate(CameraEase ease, float t)
        {
            var x = t < 0f ? 0f : t > 1f ? 1f : t;

            switch (ease)
            {
                case CameraEase.SineIn:
                    return 1f - Mathf.Cos(x * Mathf.PI * 0.5f);
                case CameraEase.SineOut:
                    return Mathf.Sin(x * Mathf.PI * 0.5f);
                case CameraEase.SineInOut:
                    return -(Mathf.Cos(Mathf.PI * x) - 1f) * 0.5f;
                case CameraEase.QuadIn:
                    return x * x;
                case CameraEase.QuadOut:
                    return 1f - (1f - x) * (1f - x);
                case CameraEase.QuadInOut:
                    return x < 0.5f ? 2f * x * x : 1f - (-2f * x + 2f) * (-2f * x + 2f) * 0.5f;
                case CameraEase.CubicOut:
                    return 1f - (1f - x) * (1f - x) * (1f - x);
                case CameraEase.BackOut:
                    var d = x - 1f;
                    return 1f + (BackOvershoot + 1f) * d * d * d + BackOvershoot * d * d;
                default:
                    return x;
            }
        }

        public static float Lerp(float from, float to, CameraEase ease, float t)
        {
            return from + (to - from) * Evaluate(ease, t);
        }

        public static Vector2 Lerp(Vector2 from, Vector2 to, CameraEase ease, float t)
        {
            var k = Evaluate(ease, t);
            return new Vector2(from.x + (to.x - from.x) * k, from.y + (to.y - from.y) * k);
        }
    }
}
