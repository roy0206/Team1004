using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Config
{
    [Serializable]
    public sealed class GameConfigValues
    {
        [SerializeField] private float[] laneY = { 1.1f, 0f, -1.1f };
        [SerializeField] private float playerX = -4.2f;
        [SerializeField] private float laneMoveDuration = 0.2f;
        [SerializeField] private float scrollSpeed = 4f;
        [SerializeField] private float[] sectionDurations = { 25f, 30f, 35f, 15f };
        [SerializeField] private float jumpDuration = 1.6f;
        [SerializeField] private float jumpCooldown = 1.2f;
        [SerializeField] private float jumpHeight = 2.2f;
        [SerializeField] private float waterSurfaceY = 2f;
        [SerializeField] private float hitStopDuration = 0.1f;
        [SerializeField] private float hitPushDistance = 0.4f;
        [SerializeField] private float bossDuration = 30f;
        [SerializeField] private float bossTelegraphDuration = 1.2f;
        [SerializeField] private float bossFullLaneTelegraphDuration = 1.5f;
        [SerializeField] private float bossAttackDuration = 0.6f;
        [SerializeField] private float bossRecoveryDuration = 0.6f;
        [SerializeField] private float bossLaneBandWidth = 12.8f;
        [SerializeField] private float bossLaneBandHeight = 0.9f;
        [SerializeField] private float bossBannerDuration = 1f;
        [SerializeField] private float bossClearBannerDuration = 1f;
        [SerializeField] private int spawnSeed;
        [SerializeField] private float playerScale = 0.29f;
        [SerializeField] private float playerHitboxScale = 0.87f;
        [SerializeField] private float controlHintDuration = 2.5f;
        [SerializeField] private bool debugEnabled;
        [SerializeField] private bool debugInvincible;

        public IReadOnlyList<float> LaneY => laneY;
        public int LaneCount => laneY?.Length ?? 0;
        public float PlayerX => playerX;
        public float LaneMoveDuration => laneMoveDuration;
        public float ScrollSpeed => scrollSpeed;
        public IReadOnlyList<float> SectionDurations => sectionDurations;
        public int SectionCount => sectionDurations?.Length ?? 0;
        public float JumpDuration => jumpDuration;
        public float JumpCooldown => jumpCooldown;
        public float JumpHeight => jumpHeight;
        public float WaterSurfaceY => waterSurfaceY;
        public float HitStopDuration => hitStopDuration;
        public float HitPushDistance => hitPushDistance;
        public float BossDuration => bossDuration;
        public float BossTelegraphDuration => bossTelegraphDuration;
        public float BossFullLaneTelegraphDuration => bossFullLaneTelegraphDuration;
        public float BossAttackDuration => bossAttackDuration;
        public float BossRecoveryDuration => bossRecoveryDuration;
        public float BossLaneBandWidth => bossLaneBandWidth;
        public float BossLaneBandHeight => bossLaneBandHeight;
        public float BossBannerDuration => bossBannerDuration;
        public float BossClearBannerDuration => bossClearBannerDuration;
        public int SpawnSeed => spawnSeed;
        public float PlayerScale => playerScale;
        public float PlayerHitboxScale => playerHitboxScale;
        public float ControlHintDuration => controlHintDuration;
        public bool DebugEnabled => debugEnabled;
        public bool DebugInvincible => debugInvincible;

        [Obsolete("Use GetSectionLength(section) or GetSectionDuration(section). StageLength returns the length of section 1.")]
        public float StageLength => GetSectionLength(1);

        public float TotalDuration
        {
            get
            {
                var total = 0f;
                if (sectionDurations == null)
                    return total;

                for (var i = 0; i < sectionDurations.Length; i++)
                    total += Mathf.Max(0f, sectionDurations[i]);

                return total;
            }
        }

        public float TotalLength => TotalDuration * Mathf.Max(0f, scrollSpeed);

        public float GetLaneY(int lane)
        {
            if (laneY == null || laneY.Length == 0)
                return 0f;

            return laneY[Mathf.Clamp(lane, 0, laneY.Length - 1)];
        }

        public float GetSectionDuration(int section)
        {
            if (sectionDurations == null || sectionDurations.Length == 0)
                return 0f;

            return Mathf.Max(0f, sectionDurations[Mathf.Clamp(section - 1, 0, sectionDurations.Length - 1)]);
        }

        public float GetSectionLength(int section)
        {
            return GetSectionDuration(section) * Mathf.Max(0f, scrollSpeed);
        }

        public GameConfigValues Clone()
        {
            return GameConfigJson.Clone(this);
        }

        public bool Validate(out string error)
        {
            if (laneY == null || laneY.Length < 1)
            {
                error = "laneY must contain at least one lane.";
                return false;
            }

            if (laneMoveDuration < 0f)
            {
                error = "laneMoveDuration must be 0 or greater.";
                return false;
            }

            if (scrollSpeed <= 0f)
            {
                error = "scrollSpeed must be greater than 0.";
                return false;
            }

            if (sectionDurations == null || sectionDurations.Length < 1)
            {
                error = "sectionDurations must contain at least one section.";
                return false;
            }

            for (var i = 0; i < sectionDurations.Length; i++)
            {
                if (sectionDurations[i] <= 0f)
                {
                    error = $"sectionDurations[{i}] must be greater than 0.";
                    return false;
                }
            }

            if (jumpDuration < 0f || jumpCooldown < 0f || jumpHeight < 0f)
            {
                error = "jumpDuration, jumpCooldown and jumpHeight must be 0 or greater.";
                return false;
            }

            if (hitStopDuration < 0f || hitPushDistance < 0f)
            {
                error = "hitStopDuration and hitPushDistance must be 0 or greater.";
                return false;
            }

            if (bossDuration <= 0f || bossTelegraphDuration < 0f || bossFullLaneTelegraphDuration < 0f ||
                bossAttackDuration < 0f || bossRecoveryDuration < 0f)
            {
                error = "bossDuration must be greater than 0 and boss phase durations must be 0 or greater.";
                return false;
            }

            if (bossLaneBandWidth <= 0f || bossLaneBandHeight <= 0f)
            {
                error = "bossLaneBandWidth and bossLaneBandHeight must be greater than 0.";
                return false;
            }

            if (bossBannerDuration < 0f || bossClearBannerDuration < 0f)
            {
                error = "bossBannerDuration and bossClearBannerDuration must be 0 or greater.";
                return false;
            }

            if (playerScale <= 0f || playerHitboxScale <= 0f)
            {
                error = "playerScale and playerHitboxScale must be greater than 0.";
                return false;
            }

            if (controlHintDuration < 0f)
            {
                error = "controlHintDuration must be 0 or greater.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
