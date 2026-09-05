using System;

namespace Game.Spawner
{
    public readonly struct DifficultySample
    {
        public DifficultySample(
            float speedMultiplier,
            float patternGap,
            float minReactionMargin,
            float jumpRequiredRatio,
            float tier1Weight,
            float tier2Weight,
            float tier3Weight,
            bool spawnEnabled)
        {
            SpeedMultiplier = Math.Max(0.01f, speedMultiplier);
            PatternGap = Math.Max(0f, patternGap);
            MinReactionMargin = Math.Max(0f, minReactionMargin);
            JumpRequiredRatio = Math.Clamp(jumpRequiredRatio, 0f, 1f);
            Tier1Weight = Math.Max(0f, tier1Weight);
            Tier2Weight = Math.Max(0f, tier2Weight);
            Tier3Weight = Math.Max(0f, tier3Weight);
            SpawnEnabled = spawnEnabled;
        }

        public float SpeedMultiplier { get; }
        public float PatternGap { get; }
        public float MinReactionMargin { get; }
        public float JumpRequiredRatio { get; }
        public float Tier1Weight { get; }
        public float Tier2Weight { get; }
        public float Tier3Weight { get; }
        public bool SpawnEnabled { get; }

        public float TierWeight(int tier)
        {
            switch (tier)
            {
                case 1: return Tier1Weight;
                case 2: return Tier2Weight;
                default: return Tier3Weight;
            }
        }

        public DifficultySample WithSpawnEnabled(bool enabled)
        {
            return new DifficultySample(
                SpeedMultiplier, PatternGap, MinReactionMargin, JumpRequiredRatio,
                Tier1Weight, Tier2Weight, Tier3Weight, enabled);
        }

        public static DifficultySample Lerp(in DifficultySample a, in DifficultySample b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new DifficultySample(
                a.SpeedMultiplier + (b.SpeedMultiplier - a.SpeedMultiplier) * t,
                a.PatternGap + (b.PatternGap - a.PatternGap) * t,
                a.MinReactionMargin + (b.MinReactionMargin - a.MinReactionMargin) * t,
                a.JumpRequiredRatio + (b.JumpRequiredRatio - a.JumpRequiredRatio) * t,
                a.Tier1Weight + (b.Tier1Weight - a.Tier1Weight) * t,
                a.Tier2Weight + (b.Tier2Weight - a.Tier2Weight) * t,
                a.Tier3Weight + (b.Tier3Weight - a.Tier3Weight) * t,
                t < 0.5f ? a.SpawnEnabled : b.SpawnEnabled);
        }
    }
}
