using System;
using System.Collections.Generic;

namespace Game.Spawner
{
    public sealed class ObstacleMix
    {
        public const float Strength = 1f;
        public const float MinShare = 0.02f;
        public const float MinMultiplier = 0.02f;
        public const float MaxMultiplier = 24f;

        private readonly string[] ids;
        private readonly float[] targets;
        private readonly int[] counts;
        private int total;

        public ObstacleMix(IReadOnlyList<ObstacleSpec> obstacles)
        {
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));

            ids = new string[obstacles.Count];
            targets = new float[obstacles.Count];
            counts = new int[obstacles.Count];

            var sum = 0f;
            for (var i = 0; i < obstacles.Count; i++)
            {
                ids[i] = obstacles[i].Id;
                targets[i] = Math.Max(0f, obstacles[i].SpawnWeight);
                sum += targets[i];
            }

            if (sum <= 0f)
            {
                for (var i = 0; i < targets.Length; i++)
                    targets[i] = targets.Length > 0 ? 1f / targets.Length : 0f;

                return;
            }

            for (var i = 0; i < targets.Length; i++)
                targets[i] /= sum;
        }

        public int TypeCount => ids.Length;
        public int Total => total;

        public static ObstacleMix FromPatterns(IReadOnlyList<PatternSpec> patterns)
        {
            var obstacles = new List<ObstacleSpec>();

            if (patterns != null)
                for (var i = 0; i < patterns.Count; i++)
                {
                    var entries = patterns[i].Entries;

                    for (var j = 0; j < entries.Count; j++)
                    {
                        var obstacle = entries[j].Obstacle;
                        if (obstacle == null || !obstacle.UsableInNormalPatterns || IndexOf(obstacles, obstacle.Id) >= 0)
                            continue;

                        obstacles.Add(obstacle);
                    }
                }

            return new ObstacleMix(obstacles);
        }

        public void Reset()
        {
            Array.Clear(counts, 0, counts.Length);
            total = 0;
        }

        public string GetId(int index)
        {
            return index >= 0 && index < ids.Length ? ids[index] : null;
        }

        public float TargetShare(int index)
        {
            return index >= 0 && index < targets.Length ? targets[index] : 0f;
        }

        public int Count(int index)
        {
            return index >= 0 && index < counts.Length ? counts[index] : 0;
        }

        public float Share(int index)
        {
            if (total <= 0 || index < 0 || index >= counts.Length)
                return 0f;

            return counts[index] / (float)total;
        }

        public float Debt(int index)
        {
            if (index < 0 || index >= counts.Length)
                return 0f;

            return targets[index] * total - counts[index];
        }

        public float Multiplier(PatternSpec pattern)
        {
            if (pattern == null || total <= 0)
                return 1f;

            var entries = pattern.Entries;
            if (entries.Count == 0)
                return 1f;

            var score = 0f;

            for (var i = 0; i < entries.Count; i++)
            {
                var obstacle = entries[i].Obstacle;
                if (obstacle == null)
                    continue;

                var index = IndexOf(obstacle.Id);
                if (index < 0)
                    continue;

                score += Debt(index);
            }

            var value = (float)Math.Exp(Strength * score);
            return Math.Clamp(value, MinMultiplier, MaxMultiplier);
        }

        public void Record(PatternPlacement placement)
        {
            if (placement == null)
                return;

            var obstacles = placement.Obstacles;

            for (var i = 0; i < obstacles.Count; i++)
            {
                var index = IndexOf(obstacles[i].Spec.Id);
                if (index < 0)
                    continue;

                counts[index]++;
                total++;
            }
        }

        private int IndexOf(string id)
        {
            for (var i = 0; i < ids.Length; i++)
                if (string.Equals(ids[i], id, StringComparison.Ordinal))
                    return i;

            return -1;
        }

        private static int IndexOf(List<ObstacleSpec> obstacles, string id)
        {
            for (var i = 0; i < obstacles.Count; i++)
                if (string.Equals(obstacles[i].Id, id, StringComparison.Ordinal))
                    return i;

            return -1;
        }
    }
}
