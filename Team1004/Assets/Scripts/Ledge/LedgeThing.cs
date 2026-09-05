using Game.Water;
using UnityEngine;

namespace Game.Ledge
{
    public sealed class LedgeThing : MonoThing
    {
        [SerializeField] private Transform hint;
        [SerializeField] private float frontOffsetX;
        [SerializeField] private float parkX = 60f;
        [SerializeField] private float hintBobAmplitude = 0.12f;
        [SerializeField] private float hintBobSpeed = 2.4f;

        private LedgeScrollModule scroll;
        private LedgeHintModule hintDisplay;
        private LedgeGateModule gate;
        private float stepHeight;

        public LedgePhase Phase => gate != null ? gate.Phase : LedgePhase.Idle;
        public float Time => gate != null ? gate.Time : 0f;
        public float FrontX => gate != null ? gate.FrontX : parkX;
        public float RiseProgress01 => gate != null ? gate.RiseProgress01 : 0f;
        public float SettleProgress01 => gate != null ? gate.SettleProgress01 : 1f;
        public bool IsActive => gate != null && gate.IsActive;
        public bool IsHoldingWorld => gate != null && gate.IsHoldingWorld;
        public bool IsSpawnSuspended => gate != null && gate.IsSpawnSuspended;
        public float VerticalOffset => scroll != null ? scroll.VerticalOffset : 0f;

        private void Awake()
        {
            EnsureModules();
        }

        public bool Schedule(in LedgePlan plan, LedgeData data, bool showHint, float startTime = 0f)
        {
            if (data == null)
                return false;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            EnsureModules();

            stepHeight = data.StepHeight;

            if (!gate.Begin(plan, data, startTime))
            {
                Retire();
                return false;
            }

            hintDisplay.SetVisible(showHint);
            scroll.Place(gate.FrontX, 0f);
            WaterInteractor.NotifyTeleport(transform);
            return true;
        }

        public LedgeSignal Advance(float deltaTime, bool playerAirborne)
        {
            if (gate == null)
                return LedgeSignal.None;

            var signal = gate.Advance(deltaTime, playerAirborne);
            scroll.Place(gate.FrontX, -stepHeight * LedgeGateModule.Ease(gate.RiseProgress01));
            return signal;
        }

        public void Retire()
        {
            EnsureModules();
            gate.Reset();
            hintDisplay.SetVisible(false);
            scroll.Park();

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void EnsureModules()
        {
            if (gate != null)
                return;

            scroll = AddModule(new LedgeScrollModule(transform, frontOffsetX, parkX));
            hintDisplay = AddModule(new LedgeHintModule(hint, hintBobAmplitude, hintBobSpeed));
            gate = AddModule(new LedgeGateModule());
        }

        protected override void OnThingDestroy()
        {
            scroll = null;
            hintDisplay = null;
            gate = null;
        }
    }
}
