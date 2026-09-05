using DG.Tweening;
using Game.StateMachine;
using UnityEngine;

namespace Game.Boss
{
    public sealed class BearBoss : PatternBoss<BearBoss, BearBossState, BearBossData>
    {
        [SerializeField] private BearArmView backArm;
        [SerializeField] private BearArmView[] sideArms;
        [SerializeField] private SpriteRenderer shadow;
        [SerializeField] private SpriteRenderer silhouette;

        private Sequence backArmTween;
        private Tween[] sideTweens;
        private Tween shadowFadeTween;
        private Tween shadowPulseTween;
        private Tween shadowMoveTween;
        private Vector3 shadowHome;
        private bool shadowHomeCached;
        private int activeSideCount;

        public BearArmView BackArm => backArm;
        public int SideArmCount => sideArms != null ? sideArms.Length : 0;
        public int ActiveSideArmCount => activeSideCount;
        public SpriteRenderer Shadow => shadow;
        public SpriteRenderer Silhouette => silhouette;

        protected override BearBossState InitialKey => BearBossState.Intro;

        public bool IsSequencePattern(BossPattern pattern)
        {
            return pattern != null && BearBossData.IsSequenceLabel(pattern.Label);
        }

        public BearArmView GetSideArm(int index)
        {
            return sideArms != null && index >= 0 && index < sideArms.Length ? sideArms[index] : null;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            CacheShadowHome();
            ResetArms();
            ResetShadow();
        }

        protected override void BuildStates(StateMachineModule<BearBoss, BearBossState> fsm)
        {
            var machine = fsm.Machine;

            fsm.Add(BearBossState.Intro, new BearIntroState(machine, Data.IntroDuration));
            fsm.Add(BearBossState.Attack, new BearAttackPhase(machine, Timer, Timing, BearBossState.Outro));
            fsm.Add(BearBossState.Outro, new BearOutroState(machine, Data.OutroDuration));
        }

        protected override void OnComplete(BossOutcome outcome)
        {
            base.OnComplete(outcome);
            ResetArms();
            ResetShadow();
        }

        protected override void OnReset()
        {
            base.OnReset();
            ResetArms();
            ResetShadow();
        }

        public void ResetArms()
        {
            KillArmTweens();
            DisarmLaneHazard();
            activeSideCount = 0;

            var parkX = Data != null ? Data.ArmParkX : 9f;
            var parkY = GetLaneY(MiddleLane);

            ParkArm(backArm, parkX, parkY);

            for (var i = 0; i < SideArmCount; i++)
                ParkArm(sideArms[i], parkX, parkY);
        }

        public void SetTelegraphMask(int laneMask)
        {
            if (Telegraph != null)
                Telegraph.ShowMask(laneMask);
        }

        public void ArmLane(int lane)
        {
            if (LaneHazard == null)
                return;

            LaneHazard.Prepare(BossLanes.Mask(lane));
            LaneHazard.Arm();
        }

        public void StrikeBackArm(int lane)
        {
            if (backArm == null || !backArm.IsValid || Data == null)
                return;

            KillBackArmTween();

            var spawnX = Data.GetLaneSpawnX(lane);
            var strikeX = Config.PlayerX + Data.BackStrikeOffset;

            backArm.SetLane(lane);
            backArm.ApplyHeight(ResolveArmHeight());
            backArm.Place(spawnX, GetLaneY(lane));
            backArm.SetVisible(true);

            var sequence = DOTween.Sequence();
            sequence.Append(backArm.Root.DOMoveX(strikeX, Data.ArmEnterDuration).SetEase(Ease.InQuad));
            sequence.Append(backArm.Root.DOMoveX(spawnX, Data.ArmReturnDuration).SetEase(Ease.OutQuad));
            sequence.OnComplete(HideBackArm);
            backArmTween = sequence;
        }

        public void StageSideArms(int laneMask)
        {
            if (Data == null)
                return;

            KillSideArmTweens();
            activeSideCount = 0;

            var height = ResolveArmHeight();

            for (var lane = 0; lane < LaneCount; lane++)
            {
                if (!BossLanes.Contains(laneMask, lane))
                    continue;

                var arm = GetSideArm(activeSideCount);

                if (arm == null || !arm.IsValid)
                    continue;

                arm.SetLane(lane);
                arm.ApplyHeight(height);
                arm.Place(Data.GetLaneSpawnX(lane), GetLaneY(lane));
                arm.SetVisible(false);
                activeSideCount++;
            }
        }

        public void SweepSideArms(float duration)
        {
            if (Data == null || activeSideCount <= 0)
                return;

            EnsureSideTweens();

            var targetX = ResolveSideSweepX();

            for (var i = 0; i < activeSideCount; i++)
            {
                var arm = GetSideArm(i);

                if (arm == null || !arm.IsValid)
                    continue;

                arm.SetVisible(true);
                KillSideTween(i);
                sideTweens[i] = arm.Root.DOMoveX(targetX, duration).SetEase(Ease.Linear);
            }
        }

        public float ResolveSideSweepX()
        {
            var playerX = Config.PlayerX;
            var offset = Data != null ? Data.SidePassOffset : 1.5f;
            return playerX - offset;
        }

        public void HideSideArms()
        {
            KillSideArmTweens();

            var parkX = Data != null ? Data.ArmParkX : 9f;

            for (var i = 0; i < SideArmCount; i++)
                ParkArm(sideArms[i], parkX, GetLaneY(MiddleLane));

            activeSideCount = 0;
        }

        public void BeginShadowFadeIn()
        {
            if (shadow == null || Data == null)
                return;

            CacheShadowHome();
            KillShadowTweens();
            shadow.transform.position = shadowHome;
            shadow.enabled = true;
            SetShadowAlpha(0f);

            var target = Data.ShadowAlpha;
            var fade = Data.ShadowFadeIn;

            if (fade <= 0f)
            {
                SetShadowAlpha(target);
                StartShadowPulse();
                return;
            }

            shadowFadeTween = DOTween
                .ToAlpha(() => shadow.color, value => shadow.color = value, target, fade)
                .SetEase(Ease.OutSine)
                .OnComplete(StartShadowPulse);
        }

        public void BeginShadowExit()
        {
            if (shadow == null || Data == null)
                return;

            KillShadowTweens();

            var fade = Data.ShadowFadeOut;

            if (fade <= 0f)
            {
                ResetShadow();
                return;
            }

            shadowFadeTween = DOTween
                .ToAlpha(() => shadow.color, value => shadow.color = value, 0f, fade)
                .SetEase(Ease.InSine);

            var distance = Data.ShadowExitDistance;

            if (Mathf.Abs(distance) > 0.0001f)
                shadowMoveTween = shadow.transform
                    .DOMoveX(shadowHome.x - distance, Data.OutroDuration)
                    .SetEase(Ease.InSine);
        }

        public void ResetShadow()
        {
            KillShadowTweens();

            if (shadow == null)
                return;

            CacheShadowHome();
            shadow.transform.position = shadowHome;
            SetShadowAlpha(0f);
        }

        private void StartShadowPulse()
        {
            if (shadow == null || Data == null)
                return;

            var amplitude = Data.ShadowPulseAmplitude;
            var period = Data.ShadowPulsePeriod;

            if (amplitude <= 0f || period <= 0f)
                return;

            var baseAlpha = Data.ShadowAlpha;
            var low = Mathf.Clamp01(baseAlpha - amplitude);
            var high = Mathf.Clamp01(baseAlpha + amplitude);

            SetShadowAlpha(low);
            shadowPulseTween = DOTween
                .ToAlpha(() => shadow.color, value => shadow.color = value, high, period * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void SetShadowAlpha(float alpha)
        {
            if (shadow == null)
                return;

            var color = shadow.color;
            color.a = alpha;
            shadow.color = color;
        }

        private void CacheShadowHome()
        {
            if (shadowHomeCached || shadow == null)
                return;

            shadowHomeCached = true;
            shadowHome = shadow.transform.position;
        }

        private float ResolveArmHeight()
        {
            var requested = Data != null ? Data.ArmHeight : 0.9f;
            var band = Config.BossLaneBandHeight;
            return band > 0f ? Mathf.Min(requested, band) : requested;
        }

        private void ParkArm(BearArmView arm, float parkX, float parkY)
        {
            if (arm == null || !arm.IsValid)
                return;

            arm.SetVisible(false);
            arm.SetLane(-1);
            arm.ApplyHeight(ResolveArmHeight());
            arm.Place(parkX, parkY);
        }

        private void HideBackArm()
        {
            backArmTween = null;

            if (backArm != null)
                backArm.SetVisible(false);
        }

        private void EnsureSideTweens()
        {
            if (sideTweens == null || sideTweens.Length != SideArmCount)
                sideTweens = new Tween[SideArmCount];
        }

        private void KillArmTweens()
        {
            KillBackArmTween();
            KillSideArmTweens();
        }

        private void KillBackArmTween()
        {
            if (backArmTween != null && backArmTween.IsActive())
                backArmTween.Kill();

            backArmTween = null;
        }

        private void KillSideArmTweens()
        {
            if (sideTweens == null)
                return;

            for (var i = 0; i < sideTweens.Length; i++)
                KillSideTween(i);
        }

        private void KillSideTween(int index)
        {
            if (sideTweens == null || index < 0 || index >= sideTweens.Length)
                return;

            var tween = sideTweens[index];

            if (tween != null && tween.IsActive())
                tween.Kill();

            sideTweens[index] = null;
        }

        private void KillShadowTweens()
        {
            KillTween(ref shadowFadeTween);
            KillTween(ref shadowPulseTween);
            KillTween(ref shadowMoveTween);
        }

        private static void KillTween(ref Tween tween)
        {
            if (tween != null && tween.IsActive())
                tween.Kill();

            tween = null;
        }
    }
}
