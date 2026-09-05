using System.Collections.Generic;
using UnityEngine;

namespace Game.Boss
{
    [CreateAssetMenu(fileName = "BearBossData", menuName = "Team1004/Boss/Bear Boss Data")]
    public sealed class BearBossData : BossData
    {
        public const string SequenceLabel = "BEAR_BACKHAND_SEQUENCE_123";
        public const string Side1Label = "BEAR_SIDE_1";
        public const string Side2Label = "BEAR_SIDE_2";
        public const string Side3Label = "BEAR_SIDE_3";
        public const string Side12Label = "BEAR_SIDE_12";
        public const string Side23Label = "BEAR_SIDE_23";

        [SerializeField] private float sequentialStepInterval = 0.5f;
        [SerializeField] private float sequenceHitActiveTime = 0.18f;
        [SerializeField] private float sequenceArmLead = 0.12f;
        [SerializeField] private float armEnterDuration = 0.12f;
        [SerializeField] private float armReturnDuration = 0.13f;
        [SerializeField] private float armHeight = 0.9f;
        [SerializeField] private float backStrikeOffset;
        [SerializeField] private float sidePassOffset = 1.5f;
        [SerializeField] private float armParkX = 9f;
        [SerializeField] private float[] laneSpawnX = { 7.5f, 7.5f, 7.5f };
        [SerializeField] private float shadowAlpha = 0.45f;
        [SerializeField] private float shadowFadeIn = 0.8f;
        [SerializeField] private float shadowFadeOut = 0.5f;
        [SerializeField] private float shadowPulseAmplitude = 0.05f;
        [SerializeField] private float shadowPulsePeriod = 2f;
        [SerializeField] private float shadowExitDistance = 12f;

        public float SequentialStepInterval => Mathf.Max(0.01f, sequentialStepInterval);
        public float SequenceHitActiveTime => Mathf.Max(0.01f, sequenceHitActiveTime);
        public float SequenceArmLead => Mathf.Max(0f, sequenceArmLead);
        public float ArmEnterDuration => Mathf.Max(0f, armEnterDuration);
        public float ArmReturnDuration => Mathf.Max(0f, armReturnDuration);
        public float ArmHeight => Mathf.Max(0.05f, armHeight);
        public float BackStrikeOffset => backStrikeOffset;
        public float SidePassOffset => sidePassOffset;
        public float ArmParkX => armParkX;
        public float ShadowAlpha => Mathf.Clamp01(shadowAlpha);
        public float ShadowFadeIn => Mathf.Max(0f, shadowFadeIn);
        public float ShadowFadeOut => Mathf.Max(0f, shadowFadeOut);
        public float ShadowPulseAmplitude => Mathf.Max(0f, shadowPulseAmplitude);
        public float ShadowPulsePeriod => Mathf.Max(0f, shadowPulsePeriod);
        public float ShadowExitDistance => shadowExitDistance;

        protected override string DefaultDisplayName => "곰";

        public float GetLaneSpawnX(int lane)
        {
            if (laneSpawnX == null || laneSpawnX.Length == 0)
                return armParkX;

            if (lane < 0)
                lane = 0;

            if (lane >= laneSpawnX.Length)
                lane = laneSpawnX.Length - 1;

            return laneSpawnX[lane];
        }

        public float GetSequenceAttackDuration(int stepCount)
        {
            if (stepCount <= 1)
                return SequenceHitActiveTime;

            return (stepCount - 1) * SequentialStepInterval + SequenceHitActiveTime;
        }

        public static bool IsSequenceLabel(string label)
        {
            return string.Equals(label, SequenceLabel, System.StringComparison.Ordinal);
        }

        protected override IEnumerable<BossPattern> CreateDefaultPatterns()
        {
            yield return new BossPattern(SequenceLabel, BossLanes.All(3), 25f);
            yield return new BossPattern(Side1Label, BossLanes.Mask(0), 15f);
            yield return new BossPattern(Side2Label, BossLanes.Mask(1), 15f);
            yield return new BossPattern(Side3Label, BossLanes.Mask(2), 15f);
            yield return new BossPattern(Side12Label, BossLanes.Mask(0, 1), 15f);
            yield return new BossPattern(Side23Label, BossLanes.Mask(1, 2), 15f);
        }
    }
}
