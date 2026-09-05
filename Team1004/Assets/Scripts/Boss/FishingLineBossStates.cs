using DG.Tweening;
using Game.StateMachine;

namespace Game.Boss
{
    public enum FishingLineBossState
    {
        Intro,
        Attack,
        Outro
    }

    public sealed class FishingLineAttackPhase : TelegraphedAttackPhase<FishingLineBoss, FishingLineBossState>
    {
        private BossPattern pattern;
        private Tween tween;

        public FishingLineAttackPhase(
            StateMachine<FishingLineBoss, FishingLineBossState> machine,
            BossTimer timer,
            BossAttackTiming timing,
            FishingLineBossState next)
            : base(machine, timer, timing, next)
        {
        }

        public BossPattern CurrentPattern => pattern;

        protected override void OnPhaseEnter(FishingLineBoss boss)
        {
            ImminentLead = boss.TelegraphImminentLead;
        }

        protected override void OnTelegraphBegin(FishingLineBoss boss)
        {
            boss.ResetHook();

            if (!boss.TryPickPattern(out pattern))
                return;

            boss.ShowTelegraph(pattern.LaneMask);
            boss.SetHookHitboxVisible(true);
        }

        protected override void OnTelegraphImminent(FishingLineBoss boss)
        {
            boss.BrightenTelegraph();
        }

        protected override void OnAttackBegin(FishingLineBoss boss)
        {
            if (pattern == null || boss.Hook == null)
                return;

            var lane = BossLanes.First(pattern.LaneMask);
            var targetY = boss.GetLaneY(lane);
            var descend = Timing.Attack * boss.Data.HookDescendRatio;

            KillTween();
            boss.PlayAttackSfx();
            tween = boss.Hook.DOMoveY(targetY, descend)
                .SetEase(Ease.InQuad)
                .OnComplete(() => boss.SetHookHazard(true));
        }

        protected override void OnAttackEnd(FishingLineBoss boss)
        {
            boss.HideTelegraph();
            KillTween();
            boss.SetHookHazard(false);
            boss.SetHookHitboxVisible(false);

            if (boss.Hook != null)
                tween = boss.Hook.DOMoveY(boss.HookParkY, Timing.Recovery).SetEase(Ease.OutQuad);
        }

        protected override void OnPhaseExit(FishingLineBoss boss)
        {
            KillTween();
            pattern = null;
            boss.HideTelegraph();
            boss.ResetHook();
        }

        private void KillTween()
        {
            if (tween != null && tween.IsActive())
                tween.Kill();

            tween = null;
        }
    }
}
