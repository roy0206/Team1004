using UnityEngine;

namespace Game.Water
{
    public interface IWaterCollider
    {
        Bounds Bounds { get; }
        float VerticalVelocity { get; }
        float HorizontalVelocity { get; }
        float SurfaceInfluence { get; }
        bool TransfersVerticalVelocity { get; }
        float WakeScale { get; }
        bool OverlapPoint(Vector2 point);
    }
}
