using DG.Tweening;
using Game.StateMachine;

namespace Game.Boss
{
    public enum WalrusBossState
    {
        Intro,
        Attack,
        Outro
    }

    public sealed class WalrusIntroState : TimedState<WalrusBoss, WalrusBossState>
    {
        private Tween tween;

        public WalrusIntroState(StateMachine<WalrusBoss, WalrusBossState> machine, float duration)
            : base(machine, duration, WalrusBossState.Attack)
        {
        }

        protected override void OnTimedEnter(WalrusBoss boss)
        {
            boss.ResetStance();
            boss.ResetPosition();
            boss.PlaceAtX(boss.Data.ExitX);
            tween = boss.transform.DOMoveX(boss.Data.RestX, Duration).SetEase(Ease.OutCubic);
        }

        public override void OnExit(WalrusBoss boss)
        {
            if (tween != null && tween.IsActive())
                tween.Kill();

            tween = null;
            boss.PlaceAtX(boss.Data.RestX);
        }
    }

    public sealed class WalrusAttackPhase : TelegraphedAttackPhase<WalrusBoss, WalrusBossState>
    {
        private BossPattern pattern;
        private Tween tween;

        public WalrusAttackPhase(
            StateMachine<WalrusBoss, WalrusBossState> machine,
            BossTimer timer,
            BossAttackTiming timing,
            WalrusBossState next)
            : base(machine, timer, timing, next)
        {
        }

        public BossPattern CurrentPattern => pattern;

        protected override void OnPhaseEnter(WalrusBoss boss)
        {
            ImminentLead = boss.TelegraphImminentLead;
        }

        protected override void OnTelegraphImminent(WalrusBoss boss)
        {
            boss.BrightenTelegraph();
        }

        protected override void OnTelegraphBegin(WalrusBoss boss)
        {
            if (!boss.TryPickPattern(out pattern))
                return;

            boss.ShowTelegraph(pattern.LaneMask);
            boss.SetStance(pattern);
            boss.SetBodyHitboxVisible(true);

            KillTween();
            var approach = Timing.Telegraph * boss.Data.ApproachRatio;
            tween = boss.transform.DOMoveY(boss.GetLaneCenterY(pattern.LaneMask), approach).SetEase(Ease.OutSine);
        }

        protected override void OnAttackBegin(WalrusBoss boss)
        {
            if (pattern == null)
                return;

            KillTween();
            boss.PlayAttackSfx();
            boss.SetBodyHazard(true);
            tween = boss.transform.DOMoveX(boss.Data.DashX, Timing.Attack).SetEase(Ease.InQuad);
        }

        protected override void OnAttackEnd(WalrusBoss boss)
        {
            boss.HideTelegraph();
            KillTween();
            boss.ResetStance();
            tween = boss.transform.DOMoveX(boss.Data.RestX, Timing.Recovery).SetEase(Ease.OutSine);
        }

        protected override void OnPhaseExit(WalrusBoss boss)
        {
            KillTween();
            pattern = null;
            boss.HideTelegraph();
            boss.ResetStance();
            boss.ResetPosition();
        }

        private void KillTween()
        {
            if (tween != null && tween.IsActive())
                tween.Kill();

            tween = null;
        }
    }

    public sealed class WalrusOutroState : State<WalrusBoss>
    {
        private Tween tween;

        public override void OnEnter(WalrusBoss boss)
        {
            boss.ResetStance();
            tween = boss.transform.DOMoveX(boss.Data.ExitX, boss.Data.OutroDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    tween = null;
                    boss.Complete(BossOutcome.Passed);
                });
        }

        public override void OnExit(WalrusBoss boss)
        {
            if (tween != null && tween.IsActive())
                tween.Kill();

            tween = null;
        }
    }
}
