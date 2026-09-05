using Game.Player;
using Game.StateMachine;
using UnityEngine;

namespace Game.Boss
{
    public sealed class FishingLineBoss : PatternBoss<FishingLineBoss, FishingLineBossState, FishingLineBossData>
    {
        [SerializeField] private Transform hook;
        [SerializeField] private Hazard hookHazard;

        public Transform Hook => hook;
        public bool IsHookHazardEnabled => hookHazard != null && hookHazard.enabled;
        public float HookParkY => Config.WaterSurfaceY + (Data != null ? Data.HookParkOffset : 1.5f);

        protected override FishingLineBossState InitialKey => FishingLineBossState.Intro;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            ResetHook();
        }

        protected override void BuildStates(StateMachineModule<FishingLineBoss, FishingLineBossState> fsm)
        {
            var machine = fsm.Machine;

            fsm.Add(FishingLineBossState.Intro,
                new TimedState<FishingLineBoss, FishingLineBossState>(machine, Data.IntroDuration, FishingLineBossState.Attack));
            fsm.Add(FishingLineBossState.Attack,
                new FishingLineAttackPhase(machine, Timer, Timing, FishingLineBossState.Outro));
            fsm.Add(FishingLineBossState.Outro,
                new CompleteOnTimeoutState<FishingLineBoss, FishingLineBossState>(machine, Data.OutroDuration, BossOutcome.Passed));
        }

        protected override void OnComplete(BossOutcome outcome)
        {
            base.OnComplete(outcome);
            ResetHook();
        }

        protected override void OnReset()
        {
            base.OnReset();
            ResetHook();
        }

        public void ResetHook()
        {
            SetHookHazard(false);
            SetHookHitboxVisible(false);

            if (hook == null)
                return;

            var position = hook.position;
            hook.position = new Vector3(Config.PlayerX, HookParkY, position.z);
        }

        public void SetHookHazard(bool enabled)
        {
            if (hookHazard != null)
                hookHazard.enabled = enabled;
        }

        public void SetHookHitboxVisible(bool visible)
        {
            HazardHitbox.SetVisible(hookHazard, visible);
        }
    }
}
