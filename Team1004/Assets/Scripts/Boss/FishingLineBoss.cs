using Game.StateMachine;
using Game.Water;
using UnityEngine;

namespace Game.Boss
{
    public sealed class FishingLineBoss : PatternBoss<FishingLineBoss, FishingLineBossState, FishingLineBossData>
    {
        [SerializeField] private Transform hook;
        [SerializeField] private SpriteRenderer hookRenderer;
        [SerializeField] private Transform line;
        [SerializeField] private SpriteRenderer lineRenderer;

        public Transform Hook => hook;
        public Transform Line => line;
        public bool IsHookVisible => hookRenderer != null && hookRenderer.enabled;
        public float HookX => hook != null ? hook.position.x : 0f;
        public float HookParkY => Config.WaterSurfaceY + (Data != null ? Data.HookParkOffset : 1.5f);
        public float LineAnchorY => Config.WaterSurfaceY + (Data != null ? Data.LineAnchorOffset : 1.2f);

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
            DisarmLaneHazard();
            SetHookVisible(false);
            PlaceHook(Data != null ? Data.HookExitX : -7.5f, HookParkY);
            WaterInteractor.NotifyTeleport(hook);
        }

        public void StageHook(float y)
        {
            SetHookVisible(true);
            PlaceHook(Data != null ? Data.HookEnterX : 6f, y);
            WaterInteractor.NotifyTeleport(hook);
        }

        public void PlaceHook(float x, float y)
        {
            if (hook != null)
            {
                var position = hook.position;
                hook.position = new Vector3(x, y, position.z);
            }

            UpdateLine();
        }

        public void SetHookVisible(bool visible)
        {
            if (hookRenderer != null)
                hookRenderer.enabled = visible;

            if (lineRenderer != null)
                lineRenderer.enabled = visible;
        }

        public void UpdateLine()
        {
            if (line == null || hook == null)
                return;

            var hookPosition = hook.position;
            var drag = Data != null ? Data.LineDragOffset : 0.9f;
            var anchor = new Vector2(hookPosition.x + drag, LineAnchorY);
            var delta = new Vector2(hookPosition.x - anchor.x, hookPosition.y - anchor.y);
            var length = delta.magnitude;

            line.position = new Vector3(anchor.x + delta.x * 0.5f, anchor.y + delta.y * 0.5f, line.position.z);

            if (length > 0.0001f)
                line.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + 90f);

            var bounds = lineRenderer != null && lineRenderer.sprite != null
                ? lineRenderer.sprite.bounds.size
                : Vector3.one;
            var width = Data != null ? Data.LineWidth : 0.06f;
            var scaleX = bounds.x > 0f ? width / bounds.x : 1f;
            var scaleY = bounds.y > 0f ? length / bounds.y : 1f;
            line.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }
}
