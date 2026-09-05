using Game.Player;

namespace Game.Boss
{
    public static class HazardHitbox
    {
        public static void SetVisible(Hazard hazard, bool visible)
        {
            if (hazard != null && hazard.TryGetComponent<HitboxView>(out var view))
                view.SetVisible(visible);
        }
    }
}
