using System;

namespace Game.Boss
{
    public interface IBearSequenceListener
    {
        void OnArmEnter(int step);

        void OnImminent(int step);

        void OnHitBegin(int step, int warnMask, int armedMask);

        void OnHitEnd(int step, int warnMask);
    }

    public sealed class BearSequenceTimeline
    {
        private readonly bool[] armStarted;
        private readonly bool[] imminentRaised;
        private readonly bool[] hitStarted;
        private readonly bool[] hitEnded;

        public BearSequenceTimeline(int stepCount, float stepInterval, float hitActiveTime, float armLead, float imminentLead)
            : this(stepCount, stepInterval, hitActiveTime, armLead, imminentLead, stepInterval)
        {
        }

        public BearSequenceTimeline(int stepCount, float stepInterval, float hitActiveTime, float armLead, float imminentLead, float firstTelegraph)
        {
            StepCount = stepCount < 1 ? 1 : stepCount;
            StepInterval = stepInterval < 0.0001f ? 0.0001f : stepInterval;
            FirstTelegraph = firstTelegraph < StepInterval ? StepInterval : firstTelegraph;
            HitActiveTime = hitActiveTime < 0f ? 0f : hitActiveTime;
            ArmLead = armLead < 0f ? 0f : armLead;
            ImminentLead = imminentLead < 0f ? 0f : imminentLead;

            armStarted = new bool[StepCount];
            imminentRaised = new bool[StepCount];
            hitStarted = new bool[StepCount];
            hitEnded = new bool[StepCount];
        }

        public int StepCount { get; }
        public float StepInterval { get; }
        public float FirstTelegraph { get; }
        public float HitActiveTime { get; }
        public float ArmLead { get; }
        public float ImminentLead { get; }

        public float TelegraphDuration => FirstTelegraph;
        public float AttackDuration => (StepCount - 1) * StepInterval + HitActiveTime;
        public float TotalDuration => TelegraphDuration + AttackDuration;
        public int FirstWarnMask => BossLanes.Mask(0);

        public float GetWarnTime(int step)
        {
            var index = Clamp(step);
            return index == 0 ? 0f : GetHitTime(index - 1);
        }

        public float GetHitTime(int step)
        {
            return FirstTelegraph + Clamp(step) * StepInterval;
        }

        public float GetHitEndTime(int step)
        {
            return GetHitTime(step) + HitActiveTime;
        }

        public float GetArmEnterTime(int step)
        {
            var time = GetHitTime(step) - ArmLead;
            return time < 0f ? 0f : time;
        }

        public float GetImminentTime(int step)
        {
            var time = GetHitTime(step) - ImminentLead;
            return time < 0f ? 0f : time;
        }

        public int GetWarnMask(int step)
        {
            var next = Clamp(step) + 1;
            return next < StepCount ? BossLanes.Mask(next) : 0;
        }

        public int GetLethalMask(float time)
        {
            var mask = 0;

            for (var step = 0; step < StepCount; step++)
                if (time >= GetHitTime(step) && time < GetHitEndTime(step))
                    mask |= BossLanes.Mask(step);

            return mask;
        }

        public int GetVisibleMask(float time)
        {
            var mask = 0;

            for (var step = 0; step < StepCount; step++)
            {
                if (time < GetWarnTime(step))
                    continue;

                if (time < GetHitEndTime(step))
                    mask |= BossLanes.Mask(step);
            }

            return mask;
        }

        public void Reset()
        {
            Array.Clear(armStarted, 0, armStarted.Length);
            Array.Clear(imminentRaised, 0, imminentRaised.Length);
            Array.Clear(hitStarted, 0, hitStarted.Length);
            Array.Clear(hitEnded, 0, hitEnded.Length);
        }

        public void Advance(float time, IBearSequenceListener listener)
        {
            for (var step = 0; step < StepCount; step++)
            {
                if (!imminentRaised[step] && time >= GetImminentTime(step))
                {
                    imminentRaised[step] = true;
                    listener?.OnImminent(step);
                }

                if (!armStarted[step] && time >= GetArmEnterTime(step))
                {
                    armStarted[step] = true;
                    listener?.OnArmEnter(step);
                }

                if (!hitStarted[step] && time >= GetHitTime(step))
                {
                    hitStarted[step] = true;
                    listener?.OnHitBegin(step, GetWarnMask(step), BossLanes.Mask(step));
                }

                if (hitStarted[step] && !hitEnded[step] && time >= GetHitEndTime(step))
                {
                    hitEnded[step] = true;
                    listener?.OnHitEnd(step, GetWarnMask(step));
                }
            }
        }

        private int Clamp(int step)
        {
            if (step < 0)
                return 0;

            return step >= StepCount ? StepCount - 1 : step;
        }
    }
}
