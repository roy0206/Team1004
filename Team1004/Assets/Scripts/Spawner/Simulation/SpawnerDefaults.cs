using System;
using System.Collections.Generic;

namespace Game.Spawner
{
    public static class SpawnerDefaults
    {
        public const string RockId = "Rock";
        public const string FishId = "Fish";
        public const string LogId = "Log";
        public const int SectionCount = 4;
        public const int DefaultSeed = 12345;
        public const float NormalSectionJumpRatio = 0f;
        public const float JumpDurationSeconds = 1.6f;

        public const float LaneSpacing = 1.1f;
        public const float PlayerHitboxWidth = 0.92f;
        public const float PlayerHitboxHeight = 0.71f;
        public const float PlayerHalfWidth = 0.46f;

        public const float RockSpawnWeight = 5f;
        public const float FishSpawnWeight = 3f;
        public const float LogSpawnWeight = 2f;

        public const float RockVisualHeight = LaneSpacing;
        public const float RockBodyLength = 2.44f;
        public const float RockCollisionLength = 2.13f;
        public const float RockCollisionHeight = 0.96f;
        public const float FishCollisionHeight = 0.39f;
        public const float LogCollisionHeight = 0.85f;

        private const int Top = 0;
        private const int Middle = 1;
        private const int Bottom = 2;

        private static readonly float[] SectionDurations = { 25f, 30f, 35f, 15f };

        public static SimulationConfig CreateConfig()
        {
            return new SimulationConfig { PlayerHalfWidth = PlayerHalfWidth };
        }

        public static float GetSectionDuration(int sectionIndex)
        {
            return SectionDurations[Math.Clamp(sectionIndex, 0, SectionDurations.Length - 1)];
        }

        public static List<ObstacleSpec> CreateObstacles()
        {
            return new List<ObstacleSpec>
            {
                new ObstacleSpec(RockId, RockId, 1, LaneMask.Of(Bottom), RockBodyLength, RockCollisionLength, RockCollisionHeight, 1.0f, RockSpawnWeight, true, true, ObstacleFitAxis.Height, RockVisualHeight),
                new ObstacleSpec(FishId, FishId, 1, LaneMask.All(3), 0.9f, 0.78f, FishCollisionHeight, 1.25f, FishSpawnWeight, true, true),
                new ObstacleSpec(LogId, LogId, 1, LaneMask.All(3), 1.8f, 1.566f, LogCollisionHeight, 0.8f, LogSpawnWeight, true, true)
            };
        }

        public static List<PatternSpec> CreatePatterns(IReadOnlyList<ObstacleSpec> obstacles)
        {
            var rock = Find(obstacles, RockId);
            var fish = Find(obstacles, FishId);
            var log = Find(obstacles, LogId);

            return new List<PatternSpec>
            {
                Pattern("Rock_Bottom", Entry(rock, Bottom, 0f)),
                Pattern("Rocks_BottomTwice", Entry(rock, Bottom, 0f), Entry(rock, Bottom, 0.9f)),
                Pattern("Rocks_BottomThrice", Entry(rock, Bottom, 0f), Entry(rock, Bottom, 0.8f), Entry(rock, Bottom, 1.6f)),
                Pattern("Fish_Top", Entry(fish, Top, 0f)),
                Pattern("Fish_Middle", Entry(fish, Middle, 0f)),
                Pattern("Fish_Bottom", Entry(fish, Bottom, 0f)),
                Pattern("Log_Top", Entry(log, Top, 0f)),
                Pattern("Log_Middle", Entry(log, Middle, 0f)),
                Pattern("Log_Bottom", Entry(log, Bottom, 0f)),

                Pattern("FishTop_RockBottom", Entry(fish, Top, 0f), Entry(rock, Bottom, 0f)),
                Pattern("FishMiddle_RockBottom", Entry(fish, Middle, 0f), Entry(rock, Bottom, 0f)),
                Pattern("LogTop_RockBottom", Entry(log, Top, 0f), Entry(rock, Bottom, 0f)),
                Pattern("LogMiddle_RockBottom", Entry(log, Middle, 0f), Entry(rock, Bottom, 0f)),
                Pattern("Fishes_TopMiddle", Entry(fish, Top, 0f), Entry(fish, Middle, 0f)),
                Pattern("Fishes_TopBottom", Entry(fish, Top, 0f), Entry(fish, Bottom, 0f)),
                Pattern("FishTop_LogMiddle", Entry(fish, Top, 0f), Entry(log, Middle, 0f)),
                Pattern("LogTop_FishMiddle", Entry(log, Top, 0f), Entry(fish, Middle, 0f)),
                Pattern("FishTop_LogBottom", Entry(fish, Top, 0f), Entry(log, Bottom, 0f)),

                Pattern("RockBottom_ThenFishTop", Entry(rock, Bottom, 0f), Entry(fish, Top, 0.7f)),
                Pattern("FishTop_ThenRockBottom", Entry(fish, Top, 0f), Entry(rock, Bottom, 0.7f)),
                Pattern("RockBottom_ThenFishMiddle", Entry(rock, Bottom, 0f), Entry(fish, Middle, 0.6f)),
                Pattern("LogTop_ThenRockBottom", Entry(log, Top, 0f), Entry(rock, Bottom, 0.6f)),
                Pattern("LogMiddle_ThenRockBottom", Entry(log, Middle, 0f), Entry(rock, Bottom, 0.6f)),
                Pattern("RockBottom_ThenLogTop", Entry(rock, Bottom, 0f), Entry(log, Top, 0.6f)),
                Pattern("RockBottom_ThenLogMiddle", Entry(rock, Bottom, 0f), Entry(log, Middle, 0.6f)),
                Pattern("Rocks_BottomTwice_FishTopBetween", Entry(rock, Bottom, 0f), Entry(fish, Top, 0.7f), Entry(rock, Bottom, 1.4f)),
                Pattern("Fish_TopThenBottom", Entry(fish, Top, 0f), Entry(fish, Bottom, 0.5f)),
                Pattern("Fish_BottomThenTop", Entry(fish, Bottom, 0f), Entry(fish, Top, 0.5f)),
                Pattern("Weave_RockBottom_ThenFishTopLogMiddle", Entry(rock, Bottom, 0f), Entry(fish, Top, 0.9f), Entry(log, Middle, 0.9f)),
                Pattern("Weave_LogMiddle_ThenFishTopRockBottom", Entry(log, Middle, 0f), Entry(fish, Top, 1.2f), Entry(rock, Bottom, 1.2f)),

                Pattern("Jump_FishTop_FishMiddle_RockBottom", true, Entry(fish, Top, 0f), Entry(fish, Middle, 0f), Entry(rock, Bottom, 0f)),
                Pattern("Jump_LogTop_FishMiddle_RockBottom", true, Entry(log, Top, 0f), Entry(fish, Middle, 0f), Entry(rock, Bottom, 0f)),
                Pattern("Jump_FishTop_LogMiddle_RockBottom", true, Entry(fish, Top, 0f), Entry(log, Middle, 0.2f), Entry(rock, Bottom, 0.2f))
            };
        }

        public static void GetSectionEndpoints(int sectionIndex, out DifficultySample start, out DifficultySample end, out float spawnStopProgress)
        {
            switch (sectionIndex)
            {
                case 0:
                    start = new DifficultySample(1.00f, 2.5f, 1.3f, NormalSectionJumpRatio, 1.0f, 0.20f, 0.00f, true);
                    end = new DifficultySample(1.00f, 2.0f, 1.3f, NormalSectionJumpRatio, 1.0f, 0.50f, 0.15f, true);
                    spawnStopProgress = 1f;
                    break;
                case 1:
                    start = new DifficultySample(1.00f, 2.0f, 1.0f, NormalSectionJumpRatio, 0.6f, 1.00f, 0.30f, true);
                    end = new DifficultySample(1.00f, 1.6f, 1.0f, NormalSectionJumpRatio, 0.4f, 1.00f, 0.60f, true);
                    spawnStopProgress = 1f;
                    break;
                case 2:
                    start = new DifficultySample(1.00f, 1.7f, 0.8f, NormalSectionJumpRatio, 0.3f, 1.00f, 0.80f, true);
                    end = new DifficultySample(1.00f, 1.3f, 0.8f, NormalSectionJumpRatio, 0.2f, 1.00f, 1.20f, true);
                    spawnStopProgress = 1f;
                    break;
                default:
                    start = new DifficultySample(1.00f, 2.5f, 1.3f, NormalSectionJumpRatio, 1.0f, 0.30f, 0.00f, true);
                    end = new DifficultySample(1.00f, 3.0f, 1.3f, NormalSectionJumpRatio, 1.0f, 0.10f, 0.00f, true);
                    spawnStopProgress = 0.7f;
                    break;
            }
        }

        public static DifficultyProfile CreateProfile()
        {
            var sections = new List<SectionProfile>(SectionCount);

            for (var i = 0; i < SectionCount; i++)
            {
                GetSectionEndpoints(i, out var start, out var end, out var stop);
                sections.Add(SectionProfile.Linear(start, end, stop));
            }

            return new DifficultyProfile(sections);
        }

        private static ObstacleSpec Find(IReadOnlyList<ObstacleSpec> obstacles, string id)
        {
            for (var i = 0; i < obstacles.Count; i++)
                if (obstacles[i].Id == id)
                    return obstacles[i];

            throw new ArgumentException("Obstacle not found: " + id, nameof(obstacles));
        }

        private static PatternEntrySpec Entry(ObstacleSpec obstacle, int lane, float arrivalOffset)
        {
            return new PatternEntrySpec(obstacle, lane, arrivalOffset);
        }

        private static PatternSpec Pattern(string id, params PatternEntrySpec[] entries)
        {
            return new PatternSpec(id, entries, false);
        }

        private static PatternSpec Pattern(string id, bool manualJumpRequired, params PatternEntrySpec[] entries)
        {
            return new PatternSpec(id, entries, manualJumpRequired);
        }
    }
}
