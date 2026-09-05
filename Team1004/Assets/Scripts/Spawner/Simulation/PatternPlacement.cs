using System;
using System.Collections.Generic;

namespace Game.Spawner
{
    public sealed class PatternPlacement
    {
        private readonly List<ObstacleTiming> obstacles;

        public PatternPlacement(
            PatternSpec pattern,
            float speedMultiplier,
            float spawnTime,
            float arrivalTime,
            List<ObstacleTiming> obstacles,
            bool isJumpRequired,
            int tier)
        {
            Pattern = pattern;
            SpeedMultiplier = speedMultiplier;
            SpawnTime = spawnTime;
            ArrivalTime = arrivalTime;
            this.obstacles = obstacles ?? new List<ObstacleTiming>();
            IsJumpRequired = isJumpRequired;
            Tier = tier;

            FirstAppearTime = spawnTime;
            LastAppearTime = spawnTime;
            FirstBlockStart = spawnTime;
            LastBlockEnd = spawnTime;

            for (var i = 0; i < this.obstacles.Count; i++)
            {
                var o = this.obstacles[i];

                if (i == 0)
                {
                    FirstAppearTime = o.AppearTime;
                    LastAppearTime = o.AppearTime;
                    FirstBlockStart = o.BlockStart;
                    LastBlockEnd = o.BlockEnd;
                    continue;
                }

                FirstAppearTime = Math.Min(FirstAppearTime, o.AppearTime);
                LastAppearTime = Math.Max(LastAppearTime, o.AppearTime);
                FirstBlockStart = Math.Min(FirstBlockStart, o.BlockStart);
                LastBlockEnd = Math.Max(LastBlockEnd, o.BlockEnd);
            }
        }

        public PatternSpec Pattern { get; }
        public float SpeedMultiplier { get; }
        public float SpawnTime { get; }
        public float ArrivalTime { get; }
        public IReadOnlyList<ObstacleTiming> Obstacles => obstacles;
        public bool IsJumpRequired { get; }
        public int Tier { get; }
        public float FirstAppearTime { get; }
        public float LastAppearTime { get; }
        public float FirstBlockStart { get; }
        public float LastBlockEnd { get; }
    }
}
