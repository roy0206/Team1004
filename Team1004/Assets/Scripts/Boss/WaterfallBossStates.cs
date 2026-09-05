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
            boss.ArmLaneHazard();

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
            boss.DisarmLaneHazard();
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
        private Tween tween;
        private bool resolved;

        public bool IsResolved => resolved;

        public override void OnEnter(WaterfallBoss boss)
        {
            if (boss.Player != null)
                boss.Player.ResetJumpCooldown();

            resolved = false;
            boss.HideTelegraph();
            boss.ParkAll();
            boss.BeginFinalApproach();
            StartApproach(boss);
        }

        public override void OnUpdate(WaterfallBoss boss, float deltaTime)
        {
            if (resolved)
                return;

            boss.AdvanceFinalApproach(deltaTime);

            if (boss.FinalApproachRemaining > 0f)
            {
                if (boss.Player != null && !boss.Player.IsAirborne)
                    boss.Player.ResetJumpCooldown();

                return;
            }

            if (boss.ClearsFinalWaterfall())
                Pass(boss);
            else
                Crash(boss);
        }

        public override void OnExit(WaterfallBoss boss)
        {
            boss.EndFinalApproach();
            KillTween();
        }

        private void StartApproach(WaterfallBoss boss)
        {
            KillTween();
            boss.PlaceWaterfall(boss.Data.FinalWaterfallEnterX);
            boss.ShowWaterfall();

            if (boss.Waterfall == null)
                return;

            tween = boss.Waterfall.DOMoveX(boss.FinalContactX, boss.Data.FinalApproachDuration)
                .SetEase(Ease.Linear);
        }

        private void Pass(WaterfallBoss boss)
        {
            resolved = true;
            boss.EndFinalApproach();
            KillTween();

            if (boss.Waterfall == null)
            {
                boss.Complete(BossOutcome.Passed);
                return;
            }

            tween = boss.Waterfall.DOMoveX(boss.Data.ExitX, boss.Data.FinalPassDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    tween = null;
                    boss.Complete(BossOutcome.Passed);
                });
        }

        private void Crash(WaterfallBoss boss)
        {
            resolved = true;
            boss.EndFinalApproach();
            KillTween();
            boss.ReportFinalWaterfallImpact();
            boss.Complete(BossOutcome.Failed);
        }

        private void KillTween()
        {
            if (tween != null && tween.IsActive())
                tween.Kill();

            tween = null;
        }
    }
}
