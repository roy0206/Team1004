using UnityEngine;

namespace Game.Ledge
{
    public sealed class LedgeClock
    {
        private const int MaxTransitionsPerStep = 8;

        private LedgePlan plan;
        private float time;
        private float riseTime;
        private float settleTime;
        private float riseDuration;
        private float settleDuration;
        private float retireX;
        private float clearMargin;
        private float qteStartDistance;

        public LedgePhase Phase { get; private set; } = LedgePhase.Idle;
        public LedgePlan Plan => plan;
        public float Time => time;
        public float RiseDuration => riseDuration;
        public float SettleDuration => settleDuration;
        public float QteStartDistance => qteStartDistance;
        public float FrontX => plan.IsValid ? plan.FrontXAt(time) : float.PositiveInfinity;
        public float RiseProgress01 => riseDuration <= 0f ? 1f : Mathf.Clamp01(riseTime / riseDuration);
        public float SettleProgress01 => settleDuration <= 0f ? 1f : Mathf.Clamp01(settleTime / settleDuration);

        public float ImpactRemaining => plan.IsValid ? Mathf.Max(0f, plan.LedgeTime - time) : 0f;
        public float FrontDistance => plan.IsValid ? FrontX - plan.PlayerX : float.PositiveInfinity;

        public bool IsActive => Phase != LedgePhase.Idle && Phase != LedgePhase.Done && Phase != LedgePhase.Failed;
        public bool IsVisible => Phase != LedgePhase.Idle && Phase != LedgePhase.Waiting && Phase != LedgePhase.Done;
        public bool IsQteActive => Phase == LedgePhase.Qte;

        public bool IsSpawnSuspended =>
            Phase == LedgePhase.Approaching ||
            Phase == LedgePhase.Qte ||
            Phase == LedgePhase.Passing ||
            Phase == LedgePhase.Rising;

        public void Reset()
        {
            plan = LedgePlan.None;
            time = 0f;
            riseTime = 0f;
            settleTime = 0f;
            riseDuration = 0f;
            settleDuration = 0f;
            retireX = 0f;
            clearMargin = 0f;
            qteStartDistance = 0f;
            Phase = LedgePhase.Idle;
        }

        public bool Begin(
            in LedgePlan schedule,
            float riseSeconds,
            float settleSeconds,
            float retirePositionX,
            float clearMarginX,
            float qteStartDistanceX,
            float startTime = 0f)
        {
            Reset();

            if (!schedule.IsValid)
                return false;

            plan = schedule;
            time = Mathf.Max(0f, startTime);
            riseDuration = Mathf.Max(0f, riseSeconds);
            settleDuration = Mathf.Max(0f, settleSeconds);
            retireX = retirePositionX;
            clearMargin = Mathf.Max(0f, clearMarginX);
            qteStartDistance = Mathf.Max(0f, qteStartDistanceX);
            Phase = LedgePhase.Waiting;
            return true;
        }

        public LedgeSignal Advance(float deltaTime, bool playerAirborne)
        {
            if (Phase == LedgePhase.Idle || Phase == LedgePhase.Done || Phase == LedgePhase.Failed)
                return LedgeSignal.None;

            var step = Mathf.Max(0f, deltaTime);

            time += step;

            if (Phase == LedgePhase.Rising)
                riseTime += step;

            if (Phase == LedgePhase.Retiring)
                settleTime += step;

            var signal = LedgeSignal.None;

            for (var guard = 0; guard < MaxTransitionsPerStep; guard++)
            {
                if (!Step(playerAirborne, ref signal))
                    break;
            }

            return signal;
        }

        private bool Step(bool playerAirborne, ref LedgeSignal signal)
        {
            switch (Phase)
            {
                case LedgePhase.Waiting:
                    if (time < plan.StartTime)
                        return false;

                    Phase = LedgePhase.Incoming;
                    signal |= LedgeSignal.Spawned;
                    return true;

                case LedgePhase.Incoming:
                    if (time < plan.ApproachTime)
                        return false;

                    Phase = LedgePhase.Approaching;
                    signal |= LedgeSignal.Approaching;
                    return true;

                case LedgePhase.Approaching:
                    if (time >= plan.LedgeTime)
                        return Arrive(playerAirborne, ref signal);

                    if (playerAirborne)
                        return false;

                    if (FrontDistance > qteStartDistance)
                        return false;

                    Phase = LedgePhase.Qte;
                    signal |= LedgeSignal.QteStarted;
                    return true;

                case LedgePhase.Qte:
                    if (playerAirborne)
                    {
                        Phase = LedgePhase.Passing;
                        signal |= LedgeSignal.Resumed;
                        return true;
                    }

                    if (time < plan.LedgeTime)
                        return false;

                    return Arrive(false, ref signal);

                case LedgePhase.Passing:
                    if (playerAirborne)
                        return false;

                    if (FrontX > plan.PlayerX - clearMargin)
                    {
                        time = Mathf.Max(time, plan.LedgeTime);
                        Phase = LedgePhase.Failed;
                        signal |= LedgeSignal.Failed;
                        return false;
                    }

                    riseTime = 0f;
                    Phase = LedgePhase.Rising;
                    signal |= LedgeSignal.Cleared;
                    return false;

                case LedgePhase.Rising:
                    if (riseTime < riseDuration)
                        return false;

                    settleTime = 0f;
                    Phase = LedgePhase.Retiring;
                    signal |= LedgeSignal.Finished;
                    return false;

                case LedgePhase.Retiring:
                    if (FrontX > retireX || settleTime < settleDuration)
                        return false;

                    Phase = LedgePhase.Done;
                    signal |= LedgeSignal.Retired;
                    return false;

                default:
                    return false;
            }
        }

        private bool Arrive(bool playerAirborne, ref LedgeSignal signal)
        {
            if (playerAirborne)
            {
                Phase = LedgePhase.Passing;
                return true;
            }

            time = plan.LedgeTime;
            Phase = LedgePhase.Failed;
            signal |= LedgeSignal.Failed;
            return false;
        }
    }
}
