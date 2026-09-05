using Game.StateMachine;
using UnityEngine;

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
        private float laneY;
        private float sweepDuration;
        private float sweepLead;
        private float sweepElapsed;
        private bool sweeping;

        public FishingLineAttackPhase(
            StateMachine<FishingLineBoss, FishingLineBossState> machine,
            BossTimer timer,
            BossAttackTiming timing,
            FishingLineBossState next)
            : base(machine, timer, timing, next)
        {
        }

        public BossPattern CurrentPattern => pattern;
        public bool IsSweeping => sweeping;
        public float SweepLead => sweepLead;

        protected override void OnPhaseEnter(FishingLineBoss boss)
        {
            ImminentLead = boss.TelegraphImminentLead;
        }

        protected override void OnTelegraphBegin(FishingLineBoss boss)
        {
            boss.ResetHook();
            sweeping = false;
            sweepElapsed = 0f;

            if (!boss.TryPickPattern(out pattern))
                return;

            boss.ShowTelegraph(pattern.LaneMask);

            laneY = boss.GetLaneY(BossLanes.First(pattern.LaneMask));
            boss.StageHook(laneY);

            var data = boss.Data;
            sweepDuration = data.HookSweepDuration;
            sweepLead = data.GetPlayerCrossRatio(boss.Config.PlayerX) * sweepDuration - Timing.Attack * 0.5f;
        }

        protected override void OnTelegraphImminent(FishingLineBoss boss)
        {
            boss.BrightenTelegraph();
        }

        protected override void OnStepUpdate(FishingLineBoss boss, float deltaTime)
        {
            if (pattern == null)
                return;

            if (!sweeping)
            {
                if (Step == BossAttackStep.Telegraph && sweepLead > 0f && CurrentTelegraph - StepElapsed <= sweepLead)
                    sweeping = true;
                else
                    return;
            }

            sweepElapsed += deltaTime;

            var data = boss.Data;
            var progress = sweepDuration > 0f ? Mathf.Clamp01(sweepElapsed / sweepDuration) : 1f;
            var x = Mathf.Lerp(data.HookEnterX, data.HookExitX, progress);
            var bob = data.HookBobAmplitude *
                      Mathf.Sin(sweepElapsed * data.HookBobFrequency * Mathf.PI * 2f);
            boss.PlaceHook(x, laneY + bob);
        }

        protected override void OnAttackBegin(FishingLineBoss boss)
        {
            if (pattern == null)
                return;

            boss.PlayAttackSfx();
            boss.ArmLaneHazard();
            sweeping = true;
        }

        protected override void OnAttackEnd(FishingLineBoss boss)
        {
            boss.DisarmLaneHazard();
            boss.HideTelegraph();
        }

        protected override void OnRecoveryEnd(FishingLineBoss boss)
        {
            boss.ResetHook();
            sweeping = false;
            sweepElapsed = 0f;
        }

        protected override void OnPhaseExit(FishingLineBoss boss)
        {
            pattern = null;
            sweeping = false;
            sweepElapsed = 0f;
            boss.HideTelegraph();
            boss.ResetHook();
        }
    }
}
