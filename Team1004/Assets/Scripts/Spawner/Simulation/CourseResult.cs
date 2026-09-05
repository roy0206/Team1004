using System.Collections.Generic;

namespace Game.Spawner
{
    public sealed class CourseResult
    {
        private readonly List<PatternPlacement> placements;

        public CourseResult(
            int sectionIndex,
            int seed,
            float durationSeconds,
            List<PatternPlacement> placements,
            GenerationStats stats,
            bool survivable,
            int finalStateCount,
            int minInputs)
        {
            SectionIndex = sectionIndex;
            Seed = seed;
            DurationSeconds = durationSeconds;
            this.placements = placements ?? new List<PatternPlacement>();
            Stats = stats;
            Survivable = survivable;
            FinalStateCount = finalStateCount;
            MinInputs = minInputs;
        }

        public int SectionIndex { get; }
        public int Seed { get; }
        public float DurationSeconds { get; }
        public IReadOnlyList<PatternPlacement> Placements => placements;
        public GenerationStats Stats { get; }
        public bool Survivable { get; }
        public int FinalStateCount { get; }
        public int MinInputs { get; }

        public int JumpRequiredCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < placements.Count; i++)
                    if (placements[i].IsJumpRequired)
                        count++;

                return count;
            }
        }
    }
}
