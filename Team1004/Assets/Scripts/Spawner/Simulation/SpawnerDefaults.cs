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

        private const int Top = 0;
        private const int Middle = 1;
        private const int Bottom = 2;

        private static readonly float[] SectionDurations = { 25f, 30f, 35f, 15f };

        public static SimulationConfig CreateConfig()
        {
            return new SimulationConfig();
        }

        public static float GetSectionDuration(int sectionIndex)
        {
            return SectionDurations[Math.Clamp(sectionIndex, 0, SectionDurations.Length - 1)];
        }

        public static List<ObstacleSpec> CreateObstacles()
        {
            return new List<ObstacleSpec>
            {
                new ObstacleSpec(RockId, RockId, 1, LaneMask.All(3), 1.0f, 0.87f, 1.0f, true, true),
                new ObstacleSpec(FishId, FishId, 1, LaneMask.All(3), 1.4f, 1.2f, 1.8f, true, true),
                new ObstacleSpec(LogId, LogId, 1, LaneMask.All(3), 3.0f, 2.6f, 1.0f, true, true)
            };
        }

        public static List<PatternSpec> CreatePatterns(IReadOnlyList<ObstacleSpec> obstacles)
        {
            var rock = Find(obstacles, RockId);
            var fish = Find(obstacles, FishId);
            var log = Find(obstacles, LogId);

            return new List<PatternSpec>
            {
                Pattern("Rock_Top", Entry(rock, Top, 0f)),
                Pattern("Rock_Middle", Entry(rock, Middle, 0f)),
                Pattern("Rock_Bottom", Entry(rock, Bottom, 0f)),
                Pattern("Fish_Top", Entry(fish, Top, 0f)),
                Pattern("Fish_Middle", Entry(fish, Middle, 0f)),
                Pattern("Fish_Bottom", Entry(fish, Bottom, 0f)),
                Pattern("Log_Top", Entry(log, Top, 0f)),
                Pattern("Log_Middle", Entry(log, Middle, 0f)),
                Pattern("Log_Bottom", Entry(log, Bottom, 0f)),
                Pattern("Rocks_TopBottom", Entry(rock, Top, 0f), Entry(rock, Bottom, 0f)),
                Pattern("Rocks_TopMiddle", Entry(rock, Top, 0f), Entry(rock, Middle, 0f)),
                Pattern("Rocks_MiddleBottom", Entry(rock, Middle, 0f), Entry(rock, Bottom, 0f)),
                Pattern("RockTop_FishBottom", Entry(rock, Top, 0f), Entry(fish, Bottom, 0f)),
                Pattern("FishTop_RockBottom", Entry(fish, Top, 0f), Entry(rock, Bottom, 0f)),
                Pattern("Fishes_TopMiddle", Entry(fish, Top, 0f), Entry(fish, Middle, 0f)),
                Pattern("RockMiddle_LogBottom", Entry(rock, Middle, 0f), Entry(log, Bottom, 0f)),
                Pattern("LogTop_RockMiddle", Entry(log, Top, 0f), Entry(rock, Middle, 0f)),
                Pattern("FishTop_LogMiddle", Entry(fish, Top, 0f), Entry(log, Middle, 0f)),
                Pattern("Rocks_TopThenBottom", Entry(rock, Top, 0f), Entry(rock, Bottom, 0.7f)),
                Pattern("Rocks_BottomThenTop", Entry(rock, Bottom, 0f), Entry(rock, Top, 0.7f)),
                Pattern("Fish_TopThenBottom", Entry(fish, Top, 0f), Entry(fish, Bottom, 0.5f)),
                Pattern("LogBottom_ThenRockMiddle", Entry(log, Bottom, 0f), Entry(rock, Middle, 0.5f)),
                Pattern("LogTop_ThenRockMiddle", Entry(log, Top, 0f), Entry(rock, Middle, 0.5f)),
                Pattern("Weave_MiddleThenTopBottom", Entry(rock, Middle, 0f), Entry(rock, Top, 0.9f), Entry(rock, Bottom, 0.9f)),
                Pattern("Jump_RocksAllLanes", true, Entry(rock, Top, 0f), Entry(rock, Middle, 0f), Entry(rock, Bottom, 0f)),
                Pattern("Jump_FishTop_RocksMiddleBottom", true, Entry(fish, Top, 0f), Entry(rock, Middle, 0f), Entry(rock, Bottom, 0f)),
                Pattern("Jump_LogBottom_RockMiddle_FishTop", true, Entry(log, Bottom, 0f), Entry(rock, Middle, 0.2f), Entry(fish, Top, 0.2f)),
                Pattern("Jump_LogMiddle_RockTop_RockBottom", true, Entry(log, Middle, 0f), Entry(rock, Top, 0.2f), Entry(rock, Bottom, 0.3f))
            };
        }

        public static void GetSectionEndpoints(int sectionIndex, out DifficultySample start, out DifficultySample end, out float spawnStopProgress)
        {
            switch (sectionIndex)
            {
                case 0:
                    start = new DifficultySample(1.00f, 2.5f, 0.6f, 0.10f, 1.0f, 0.3f, 0.0f, true);
                    end = new DifficultySample(1.05f, 2.0f, 0.6f, 0.20f, 1.0f, 0.5f, 0.1f, true);
                    spawnStopProgress = 1f;
                    break;
                case 1:
                    start = new DifficultySample(1.10f, 2.0f, 0.5f, 0.25f, 0.6f, 1.0f, 0.3f, true);
                    end = new DifficultySample(1.25f, 1.6f, 0.5f, 0.25f, 0.5f, 1.0f, 0.5f, true);
                    spawnStopProgress = 1f;
                    break;
                case 2:
                    start = new DifficultySample(1.30f, 1.7f, 0.4f, 0.30f, 0.3f, 1.0f, 1.0f, true);
                    end = new DifficultySample(1.50f, 1.3f, 0.4f, 0.35f, 0.3f, 1.0f, 1.0f, true);
                    spawnStopProgress = 1f;
                    break;
                default:
                    start = new DifficultySample(1.10f, 2.5f, 0.6f, 0.10f, 1.0f, 0.3f, 0.0f, true);
                    end = new DifficultySample(1.00f, 3.0f, 0.6f, 0.00f, 1.0f, 0.1f, 0.0f, true);
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
