using UnityEngine;

namespace Game.Water
{
    public interface IWaterCollider
    {
        Bounds Bounds { get; }
        float VerticalVelocity { get; }
        bool OverlapPoint(Vector2 point);
    }
}
