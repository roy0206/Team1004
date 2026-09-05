using UnityEngine;

namespace Game.Ledge
{
    public sealed class LedgeGateModule : Module
    {
        private readonly LedgeClock clock = new();

        public LedgeClock Clock => clock;
        public LedgePhase Phase => clock.Phase;
        public float FrontX => clock.FrontX;
        public float FrontDistance => clock.FrontDistance;
        public float Time => clock.Time;
        public float ImpactRemaining => clock.ImpactRemaining;
        public float RiseProgress01 => clock.RiseProgress01;
        public float SettleProgress01 => clock.SettleProgress01;
        public bool IsActive => clock.IsActive;
        public bool IsVisible => clock.IsVisible;
        public bool IsQteActive => clock.IsQteActive;
        public bool IsSpawnSuspended => clock.IsSpawnSuspended;

        protected override ModuleTick Ticks => ModuleTick.None;

        public bool Begin(in LedgePlan plan, LedgeData data, float startTime = 0f)
        {
            if (data == null)
                return false;

            return clock.Begin(
                plan,
                data.CameraMoveDuration,
                data.EnvironmentSettleDuration,
                data.RetireX,
                data.ClearMargin,
                data.QteStartDistance,
                startTime);
        }

        public LedgeSignal Advance(float deltaTime, bool playerAirborne)
        {
            return clock.Advance(deltaTime, playerAirborne);
        }

        public void Reset()
        {
            clock.Reset();
        }

        public static float Ease(float t)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
        }
    }
}
