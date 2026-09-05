using System;
using System.Collections.Generic;

namespace Game.Spawner
{
    public static class PlacementBuilder
    {
        public static float EarliestArrivalDelay(PatternSpec pattern, SimulationConfig config, float speedMultiplier)
        {
            var delay = 0f;
            var entries = pattern.Entries;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var own = config.EarliestArrivalDelay(entry.Obstacle, speedMultiplier) - entry.ArrivalOffset;
                delay = Math.Max(delay, own);
            }

            return delay;
        }

        public static PatternPlacement Build(
            PatternSpec pattern,
            SimulationConfig config,
            float speedMultiplier,
            float spawnTime,
            float arrivalTime,
            bool isJumpRequired,
            int tier)
        {
            var entries = pattern.Entries;
            var obstacles = new List<ObstacleTiming>(entries.Count);

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var spec = entry.Obstacle;
                var speed = config.ApproachSpeed(spec, speedMultiplier);
                var arrival = arrivalTime + entry.ArrivalOffset;
                var spawnCenterX = config.PlayerX + config.PlayerHalfWidth + spec.HalfCollision + speed * (arrival - spawnTime);
                var appearDelay = Math.Max(0f, (spawnCenterX - spec.HalfBody - config.ScreenRightX) / speed);
                var blockEnd = spawnTime + (spawnCenterX + spec.HalfCollision - config.PlayerX + config.PlayerHalfWidth) / speed;

                obstacles.Add(new ObstacleTiming(
                    spec,
                    entry.StartLane,
                    spawnTime,
                    spawnCenterX,
                    speed,
                    spawnTime + appearDelay,
                    arrival,
                    blockEnd));
            }

            return new PatternPlacement(pattern, speedMultiplier, spawnTime, arrivalTime, obstacles, isJumpRequired, tier);
        }
    }
}
