using Game.StateMachine;

namespace Game.Boss
{
    public enum BearBossState
    {
        Intro,
        Attack,
        Outro
    }

    public sealed class BearIntroState : TimedState<BearBoss, BearBossState>
    {
        public BearIntroState(StateMachine<BearBoss, BearBossState> machine, float duration)
            : base(machine, duration, BearBossState.Attack)
        {
        }

        protected override void OnTimedEnter(BearBoss boss)
        {
            boss.ResetArms();
            boss.HideTelegraph();
            boss.BeginShadowFadeIn();
        }
    }

    public sealed class BearOutroState : CompleteOnTimeoutState<BearBoss, BearBossState>
    {
        public BearOutroState(StateMachine<BearBoss, BearBossState> machine, float duration)
            : base(machine, duration, BossOutcome.Passed)
        {
        }

        protected override void OnTimedEnter(BearBoss boss)
        {
            boss.ResetArms();
            boss.HideTelegraph();
            boss.BeginShadowExit();
        }
    }

    public sealed class BearAttackPhase : TelegraphedAttackPhase<BearBoss, BearBossState>, IBearSequenceListener
    {
        private BossPattern pattern;
        private BearSequenceTimeline timeline;
        private BearBoss owner;
        private bool isSequence;

        public BearAttackPhase(
            StateMachine<BearBoss, BearBossState> machine,
            BossTimer timer,
            BossAttackTiming timing,
            BearBossState next)
            : base(machine, timer, timing, next)
        {
        }

        public BossPattern CurrentPattern => pattern;
        public bool IsSequenceCycle => isSequence;
        public BearSequenceTimeline Timeline => timeline;

        protected override void OnPhaseEnter(BearBoss boss)
        {
            owner = boss;
            ImminentLead = boss.TelegraphImminentLead;
        }

        protected override void OnTelegraphImminent(BearBoss boss)
        {
            boss.BrightenTelegraph();
        }

        protected override void OnTelegraphBegin(BearBoss boss)
        {
            owner = boss;
            isSequence = false;
            timeline = null;

            if (!boss.TryPickPattern(out pattern))
                return;

            boss.ResetArms();
            isSequence = boss.IsSequencePattern(pattern);

            if (isSequence)
            {
                var data = boss.Data;
                timeline = new BearSequenceTimeline(
                    boss.LaneCount,
                    data.SequentialStepInterval,
                    data.SequenceHitActiveTime,
                    data.SequenceArmLead,
                    boss.TelegraphImminentLead,
                    boss.Timing.Telegraph);

                Timing = new BossAttackTiming(
                    timeline.TelegraphDuration,
                    timeline.AttackDuration,
                    data.RecoveryDuration,
                    timeline.TelegraphDuration);

                boss.ShowTelegraph(timeline.FirstWarnMask);
                return;
            }

            Timing = boss.Timing;
            boss.ShowTelegraph(pattern.LaneMask);
            boss.StageSideArms(pattern.LaneMask);
        }

        protected override void OnStepUpdate(BearBoss boss, float deltaTime)
        {
            if (!isSequence || timeline == null)
                return;

            owner = boss;
            var time = ResolveSequenceTime();

            if (time < 0f)
                return;

            timeline.Advance(time, this);
        }

        protected override void OnAttackBegin(BearBoss boss)
        {
            if (pattern == null)
                return;

            if (isSequence)
                return;

            boss.PlayAttackSfx();
            boss.ArmLaneHazard();
            boss.SweepSideArms(Timing.Attack);
        }

        protected override void OnAttackEnd(BearBoss boss)
        {
            boss.DisarmLaneHazard();
            boss.HideTelegraph();
            boss.ResetArms();
        }

        protected override void OnPhaseExit(BearBoss boss)
        {
            pattern = null;
            timeline = null;
            isSequence = false;
            boss.ResetArms();
            boss.HideTelegraph();
        }

        void IBearSequenceListener.OnArmEnter(int step)
        {
            if (owner != null)
                owner.StrikeBackArm(step);
        }

        void IBearSequenceListener.OnImminent(int step)
        {
            if (owner != null)
                owner.BrightenTelegraph();
        }

        void IBearSequenceListener.OnHitBegin(int step, int warnMask, int armedMask)
        {
            if (owner == null)
                return;

            if (warnMask != 0)
                owner.ShowTelegraph(warnMask | armedMask);

            owner.ArmLane(step);
            owner.PlayAttackSfx();
        }

        void IBearSequenceListener.OnHitEnd(int step, int warnMask)
        {
            if (owner == null)
                return;

            owner.DisarmLaneHazard();

            if (warnMask != 0)
                owner.SetTelegraphMask(warnMask);
            else
                owner.HideTelegraph();
        }

        private float ResolveSequenceTime()
        {
            switch (Step)
            {
                case BossAttackStep.Telegraph:
                    return StepElapsed;
                case BossAttackStep.Attack:
                    return timeline.TelegraphDuration + StepElapsed;
                default:
                    return -1f;
            }
        }
    }
}
