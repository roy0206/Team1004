using Game.StateMachine;

namespace Game.Boss
{
    public enum FishingLineBossState
    {
        Intro,
        Attack,
        Outro
    }

    public sealed class FishingLineIntroState : TimedState<FishingLineBoss, FishingLineBossState>
    {
        public FishingLineIntroState(
            StateMachine<FishingLineBoss, FishingLineBossState> machine,
            float duration,
            FishingLineBossState next)
            : base(machine, duration, next)
        {
        }

        protected override void OnTimedEnter(FishingLineBoss boss)
        {
            boss.BeginBoatEnter();
        }
    }

    public sealed class FishingLineOutroState : TimedState<FishingLineBoss, FishingLineBossState>
    {
        public FishingLineOutroState(
            StateMachine<FishingLineBoss, FishingLineBossState> machine,
            float duration)
            : base(machine, duration, default)
        {
        }

        protected override void OnTimedEnter(FishingLineBoss boss)
        {
            boss.BeginBoatExit();
        }

        protected override void OnTimeout(FishingLineBoss boss)
        {
            boss.Complete(BossOutcome.Passed);
        }
    }

    public sealed class FishingLineAttackPhase : TelegraphedAttackPhase<FishingLineBoss, FishingLineBossState>
    {
        private BossPattern pattern;
        private float reelElapsed;
        private bool reeling;

        public FishingLineAttackPhase(
            StateMachine<FishingLineBoss, FishingLineBossState> machine,
            BossTimer timer,
            BossAttackTiming timing,
            FishingLineBossState next)
            : base(machine, timer, timing, next)
        {
        }

        public BossPattern CurrentPattern => pattern;
        public bool IsReeling => reeling;

        protected override void OnPhaseEnter(FishingLineBoss boss)
        {
            ImminentLead = boss.TelegraphImminentLead;
        }

        protected override void OnTelegraphBegin(FishingLineBoss boss)
        {
            reeling = false;
            reelElapsed = 0f;
            boss.EndCast();

            if (!boss.TryPickPattern(out pattern))
                return;

            boss.ShowTelegraph(pattern.LaneMask);
        }

        protected override void OnTelegraphImminent(FishingLineBoss boss)
        {
            boss.BrightenTelegraph();
        }

        protected override void OnAttackBegin(FishingLineBoss boss)
        {
            if (pattern == null)
                return;

            boss.PlayAttackSfx();
            boss.ArmLaneHazard();
            boss.BeginCast(pattern.LaneMask);
            boss.UpdateCast(0f);
        }

        protected override void OnStepUpdate(FishingLineBoss boss, float deltaTime)
        {
            if (pattern == null || boss.Data == null)
                return;

            if (Step == BossAttackStep.Attack)
            {
                boss.UpdateCast(StepElapsed / boss.Data.CastDuration);
                return;
            }

            if (Step != BossAttackStep.Recovery || !reeling)
                return;

            reelElapsed += deltaTime;
            var progress = reelElapsed / boss.Data.ReelDuration;
            boss.UpdateReel(progress);

            if (progress < 1f)
                return;

            reeling = false;
            boss.EndCast();
        }

        protected override void OnAttackEnd(FishingLineBoss boss)
        {
            boss.DisarmLaneHazard();
            boss.HideTelegraph();

            if (pattern == null)
                return;

            boss.BeginReel();
            reeling = true;
            reelElapsed = 0f;
        }

        protected override void OnRecoveryEnd(FishingLineBoss boss)
        {
            reeling = false;
            reelElapsed = 0f;
            boss.EndCast();
        }

        protected override void OnPhaseExit(FishingLineBoss boss)
        {
            pattern = null;
            reeling = false;
            reelElapsed = 0f;
            boss.HideTelegraph();
            boss.ResetHooks();
        }
    }
}
