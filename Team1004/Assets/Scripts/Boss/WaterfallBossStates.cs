using System.Collections.Generic;
using DG.Tweening;
using Game.StateMachine;

namespace Game.Boss
{
    public enum WaterfallBossState
    {
        Intro,
        Attack,
        Breakthrough
    }

    public sealed class WaterfallAttackPhase : TelegraphedAttackPhase<WaterfallBoss, WaterfallBossState>
    {
        private readonly List<Tween> tweens = new();
        private BossPattern pattern;

        public WaterfallAttackPhase(
            StateMachine<WaterfallBoss, WaterfallBossState> machine,
            BossTimer timer,
            BossAttackTiming timing,
            WaterfallBossState next)
            : base(machine, timer, timing, next)
        {
        }

        public BossPattern CurrentPattern => pattern;

        protected override void OnPhaseEnter(WaterfallBoss boss)
        {
            ImminentLead = boss.TelegraphImminentLead;
        }

        protected override void OnTelegraphBegin(WaterfallBoss boss)
        {
            var lead = Timing.FullLaneTelegraph;

            if (!boss.TryPickPattern(candidate => !candidate.RequiresJump || boss.CanPlayerJumpWithin(lead), out pattern))
                return;

            boss.ShowTelegraph(pattern.LaneMask);
        }

        protected override float GetTelegraphDuration(WaterfallBoss boss)
        {
            if (pattern != null && (pattern.RequiresJump || pattern.LaneCount >= boss.LaneCount))
                return Timing.FullLaneTelegraph;

            return Timing.Telegraph;
        }

        protected override void OnTelegraphImminent(WaterfallBoss boss)
        {
            boss.BrightenTelegraph();
        }

        protected override void OnAttackBegin(WaterfallBoss boss)
        {
            if (pattern == null)
                return;

            KillTweens();
            boss.PlayAttackSfx();

            for (var lane = 0; lane < boss.AttackLaneCount; lane++)
            {
                if (!pattern.Contains(lane))
                    continue;

                boss.LaunchLane(lane);

                var rapid = boss.GetRapid(lane);
                var rock = boss.GetRock(lane);

                if (rapid != null)
                    tweens.Add(rapid.DOMoveX(boss.Data.ExitX, Timing.Attack).SetEase(Ease.Linear));

                if (rock != null)
                    tweens.Add(rock.DOMoveX(boss.Data.ExitX + boss.Data.RockTrail, Timing.Attack).SetEase(Ease.Linear));
            }
        }

        protected override void OnAttackEnd(WaterfallBoss boss)
        {
            boss.HideTelegraph();
            KillTweens();
            boss.ParkAll();
        }

        protected override void OnPhaseExit(WaterfallBoss boss)
        {
            KillTweens();
            pattern = null;
            boss.HideTelegraph();
            boss.ParkAll();
        }

        private void KillTweens()
        {
            for (var i = 0; i < tweens.Count; i++)
            {
                var tween = tweens[i];

                if (tween != null && tween.IsActive())
                    tween.Kill();
            }

            tweens.Clear();
        }
    }

    public sealed class WaterfallBreakthroughState : State<WaterfallBoss>
    {
        private WaterfallBoss boss;
        private Tween tween;
        private float elapsed;
        private bool jumped;
        private bool cueShown;
        private bool listening;

        public bool HasJumped => jumped;

        public override void OnEnter(WaterfallBoss boss)
        {
            if (boss.Player != null)
                boss.Player.ResetJumpCooldown();

            this.boss = boss;
            elapsed = 0f;
            jumped = false;
            cueShown = false;
            boss.HideTelegraph();
            boss.ParkAll();
            EnterWaterfall(boss);

            if (boss.Player != null)
            {
                boss.Player.Jumped += OnPlayerJumped;
                listening = true;
            }
        }

        public override void OnUpdate(WaterfallBoss boss, float deltaTime)
        {
            if (jumped)
                return;

            elapsed += deltaTime;

            if (elapsed < boss.Data.SafeWindowDuration)
                return;

            if (boss.Player == null)
            {
                Breakthrough(boss);
                return;
            }

            if (!cueShown && boss.CanPlayerJumpWithin(0f))
            {
                cueShown = true;
                boss.ShowJumpCue();
            }
        }

        public override void OnExit(WaterfallBoss boss)
        {
            StopListening();
            KillTween();
            boss.HideJumpCue();
            this.boss = null;
        }

        private void EnterWaterfall(WaterfallBoss target)
        {
            KillTween();
            target.HideWaterfall();
            target.ShowWaterfall();

            if (target.Waterfall == null)
                return;

            tween = target.Waterfall.DOMoveX(target.Data.WaterfallX, target.Data.FinalWaterfallEnterDuration)
                .SetEase(Ease.OutCubic);
        }

        private void OnPlayerJumped()
        {
            if (boss == null || jumped)
                return;

            Breakthrough(boss);
        }

        private void Breakthrough(WaterfallBoss target)
        {
            jumped = true;
            StopListening();
            target.HideJumpCue();
            KillTween();

            if (target.Waterfall == null)
            {
                target.Complete(BossOutcome.Passed);
                return;
            }

            tween = target.Waterfall.DOMoveX(target.Data.ExitX, target.Config.JumpDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    tween = null;
                    target.Complete(BossOutcome.Passed);
                });
        }

        private void KillTween()
        {
            if (tween != null && tween.IsActive())
                tween.Kill();

            tween = null;
        }

        private void StopListening()
        {
            if (!listening)
                return;

            listening = false;

            if (boss != null && boss.Player != null)
                boss.Player.Jumped -= OnPlayerJumped;
        }
    }
}
