using System;
using Game.StateMachine;
using UnityEngine;

namespace Game.Boss
{
    public sealed class FishingLineBoss : PatternBoss<FishingLineBoss, FishingLineBossState, FishingLineBossData>
    {
        [SerializeField] private Transform boat;
        [SerializeField] private SpriteRenderer boatRenderer;
        [SerializeField] private FishingLineHookRig[] hooks = Array.Empty<FishingLineHookRig>();

        private BoatFloatModule floatModule;
        private float boatSlide;
        private float boatSlideTarget;
        private int castingHooks;

        public Transform Boat => boat;
        public int HookCount => hooks != null ? hooks.Length : 0;
        public int CastingHookCount => castingHooks;
        public float BoatSlide => boatSlide;
        public float HookHalfHeight { get; private set; }
        public float LineSlack { get; private set; } = 1f;
        public bool IsBoatVisible => boatRenderer != null && boatRenderer.enabled;

        public float BoatOffscreenX => Data != null ? Data.BoatX + Data.BoatEnterOffsetX : 0f;

        public Vector2 RodTip
        {
            get
            {
                if (boat != null && Data != null)
                    return boat.TransformPoint(Data.RodTipOffset);

                var x = Data != null ? Data.BoatX : 0f;
                return new Vector2(x, Config.WaterSurfaceY + (Data != null ? Data.LineAnchorOffset : 1.2f));
            }
        }

        public Vector2 HookRestPosition
        {
            get
            {
                var tip = RodTip;
                return new Vector2(tip.x, tip.y - (Data != null ? Data.HookRestOffset : 0.3f));
            }
        }

        protected override FishingLineBossState InitialKey => FishingLineBossState.Intro;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            ApplyHookVisuals();
            EnsureFloatModule();
            boatSlide = 0f;
            boatSlideTarget = 0f;
            PlaceBoat();
            floatModule?.Snap();
            SetBoatVisible(false);
            ResetHooks();
        }

        protected override void BuildStates(StateMachineModule<FishingLineBoss, FishingLineBossState> fsm)
        {
            var machine = fsm.Machine;

            fsm.Add(FishingLineBossState.Intro,
                new FishingLineIntroState(machine, Data.IntroDuration, FishingLineBossState.Attack));
            fsm.Add(FishingLineBossState.Attack,
                new FishingLineAttackPhase(machine, Timer, Timing, FishingLineBossState.Outro));
            fsm.Add(FishingLineBossState.Outro,
                new FishingLineOutroState(machine, Data.OutroDuration));
        }

        protected override void OnComplete(BossOutcome outcome)
        {
            base.OnComplete(outcome);
            SetBoatVisible(false);
            ResetHooks();
        }

        protected override void OnReset()
        {
            base.OnReset();
            boatSlide = 0f;
            boatSlideTarget = 0f;
            PlaceBoat();
            SetBoatVisible(false);
            ResetHooks();
        }

        protected override void OnThingUpdate()
        {
            base.OnThingUpdate();
            UpdateBoatSlide(Time.deltaTime);
        }

        protected override void OnThingLateUpdate()
        {
            base.OnThingLateUpdate();
            UpdateLines();
        }

        public void BeginBoatEnter()
        {
            boatSlideTarget = 1f;
            SetBoatVisible(true);
            SetHookVisible(0, true);
        }

        public void BeginBoatExit()
        {
            boatSlideTarget = 0f;
        }

        public void ResetHooks()
        {
            DisarmLaneHazard();
            castingHooks = 0;
            LineSlack = 1f;

            if (hooks == null)
                return;

            var rest = HookRestPosition;

            for (var i = 0; i < hooks.Length; i++)
            {
                var rig = hooks[i];

                if (rig == null || !rig.IsValid)
                    continue;

                rig.EndCast();
                rig.Position = rest;
                rig.SetVisible(i == 0 && IsBoatVisible);
                rig.NotifyTeleport();
            }
        }

        public int BeginCast(int laneMask)
        {
            castingHooks = 0;
            LineSlack = 1f;

            if (hooks == null)
                return 0;

            var rest = HookRestPosition;

            for (var lane = 0; lane < LaneCount; lane++)
            {
                if (!BossLanes.Contains(laneMask, lane))
                    continue;

                if (castingHooks >= hooks.Length)
                    break;

                var rig = hooks[castingHooks];

                if (rig == null || !rig.IsValid)
                    continue;

                rig.Position = rest;
                rig.SetVisible(true);
                rig.BeginCast(rest, GetLaneY(lane) + HookHalfHeight);
                castingHooks++;
            }

            return castingHooks;
        }

        public void UpdateCast(float progress)
        {
            if (hooks == null || Data == null)
                return;

            var t = Mathf.Clamp01(progress);
            var inverse = 1f - t;
            var horizontal = 1f - inverse * inverse;
            var vertical = 1f - inverse * inverse * inverse;
            var arc = Data.CastArcHeight * Mathf.Sin(Mathf.PI * t);
            var targetX = Config.PlayerX;

            for (var i = 0; i < hooks.Length; i++)
            {
                var rig = hooks[i];

                if (rig == null || !rig.IsCasting)
                    continue;

                var origin = rig.Origin;
                var x = Mathf.Lerp(origin.x, targetX, horizontal);
                var y = Mathf.Lerp(origin.y, rig.TargetY, vertical) + arc;
                rig.Position = new Vector2(x, y);
            }
        }

        public void BeginReel()
        {
            if (hooks == null)
                return;

            for (var i = 0; i < hooks.Length; i++)
            {
                var rig = hooks[i];

                if (rig != null && rig.IsCasting)
                    rig.BeginReel();
            }
        }

        public void UpdateReel(float progress)
        {
            if (hooks == null)
                return;

            var t = Mathf.Clamp01(progress);
            LineSlack = 1f - t;

            var rest = HookRestPosition;

            for (var i = 0; i < hooks.Length; i++)
            {
                var rig = hooks[i];

                if (rig == null || !rig.IsCasting)
                    continue;

                rig.Position = Vector2.Lerp(rig.Origin, rest, t * t);
            }
        }

        public void EndCast()
        {
            castingHooks = 0;
            LineSlack = 1f;

            if (hooks == null)
                return;

            var rest = HookRestPosition;

            for (var i = 0; i < hooks.Length; i++)
            {
                var rig = hooks[i];

                if (rig == null || !rig.IsValid)
                    continue;

                rig.EndCast();
                rig.Position = rest;
                rig.SetVisible(i == 0 && IsBoatVisible);
            }
        }

        public void SetBoatVisible(bool visible)
        {
            if (boatRenderer != null)
                boatRenderer.enabled = visible;

            if (visible || hooks == null)
                return;

            for (var i = 0; i < hooks.Length; i++)
                hooks[i]?.SetVisible(false);
        }

        private void SetHookVisible(int index, bool visible)
        {
            if (hooks == null || index < 0 || index >= hooks.Length)
                return;

            hooks[index]?.SetVisible(visible);
        }

        private void ApplyHookVisuals()
        {
            HookHalfHeight = 0f;

            if (hooks == null || Data == null)
                return;

            var scale = Data.HookScale;

            for (var i = 0; i < hooks.Length; i++)
            {
                var rig = hooks[i];

                if (rig == null || !rig.IsValid)
                    continue;

                rig.ApplyScale(scale);
                rig.ApplyBody(scale);
                rig.ApplyLineStyle(Data.LineWidth, Data.LineColor);

                if (HookHalfHeight <= 0f)
                    HookHalfHeight = rig.SpriteSize.y * scale * 0.5f;
            }
        }

        private void UpdateBoatSlide(float deltaTime)
        {
            if (boat == null || Data == null)
                return;

            var step = deltaTime / Data.BoatEnterDuration;
            boatSlide = Mathf.MoveTowards(boatSlide, boatSlideTarget, step);
            PlaceBoat();

            if (boatSlideTarget <= 0f && boatSlide <= 0f && IsBoatVisible)
                SetBoatVisible(false);
        }

        private void PlaceBoat()
        {
            if (boat == null || Data == null)
                return;

            var eased = boatSlide * boatSlide * (3f - 2f * boatSlide);
            var x = Mathf.Lerp(BoatOffscreenX, Data.BoatX, eased);
            var position = boat.position;
            boat.position = new Vector3(x, position.y, position.z);
        }

        private void UpdateLines()
        {
            if (hooks == null || Data == null)
                return;

            var rodTip = RodTip;
            var rest = HookRestPosition;
            var slack = Data.LineSag * Mathf.Clamp01(LineSlack);
            var segments = Data.LineSegments;

            for (var i = 0; i < hooks.Length; i++)
            {
                var rig = hooks[i];

                if (rig == null || !rig.IsValid)
                    continue;

                if (!rig.IsCasting)
                    rig.Position = rest;

                if (!rig.IsVisible)
                    continue;

                var length = Vector2.Distance(rodTip, rig.Position);
                rig.DrawLine(rodTip, slack * length, segments);
            }
        }

        private void EnsureFloatModule()
        {
            if (boat == null || Data == null)
                return;

            floatModule ??= AddModule(new BoatFloatModule(boat));
            floatModule.FloatOffset = Data.BoatFloatOffset;
            floatModule.TiltScale = Data.BoatTiltScale;
            floatModule.SlopeSpan = Data.BoatSlopeSpan;
            floatModule.Smoothing = Data.BoatFloatSmoothing;
            floatModule.FallbackHeight = Config.WaterSurfaceY;
            floatModule.Snap();
        }
    }
}
