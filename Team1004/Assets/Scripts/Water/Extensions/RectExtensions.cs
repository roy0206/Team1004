using UnityEngine;

namespace Game.Water
{
    public static class RectExtensions
    {
        public static float SqrDistance(this Rect rect, Vector2 point)
        {
            point -= rect.center;
            point = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - rect.size / 2;
            if (point.x < 0 && point.y < 0) return 0f;
            return new Vector2(Mathf.Max(0, point.x), Mathf.Max(0, point.y)).sqrMagnitude;
        }
    }
}
