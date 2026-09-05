using System;
using System.Collections.Generic;

namespace Game.Spawner
{
    public readonly struct PatternEntrySpec
    {
        public PatternEntrySpec(ObstacleSpec obstacle, int startLane, float arrivalOffset)
        {
            Obstacle = obstacle;
            StartLane = startLane;
            ArrivalOffset = Math.Max(0f, arrivalOffset);
        }

        public ObstacleSpec Obstacle { get; }
        public int StartLane { get; }
        public float ArrivalOffset { get; }
    }

    public sealed class PatternSpec
    {
        private readonly List<PatternEntrySpec> entries;

        public PatternSpec(string id, IEnumerable<PatternEntrySpec> entries, bool manualJumpRequired = false)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Pattern id is empty.", nameof(id));

            Id = id;
            this.entries = entries != null ? new List<PatternEntrySpec>(entries) : new List<PatternEntrySpec>();
            ManualJumpRequired = manualJumpRequired;
        }

        public string Id { get; }
        public IReadOnlyList<PatternEntrySpec> Entries => entries;
        public bool ManualJumpRequired { get; }

        public bool Validate(int laneCount, out string error)
        {
            if (entries.Count == 0)
            {
                error = "Pattern '" + Id + "' has no entries.";
                return false;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (entry.Obstacle == null)
                {
                    error = "Pattern '" + Id + "' entry " + i + " has no obstacle.";
                    return false;
                }

                if (!entry.Obstacle.CanStartAt(entry.StartLane, laneCount))
                {
                    error = "Pattern '" + Id + "' entry " + i + ": '" + entry.Obstacle.Id + "' cannot start at lane " + entry.StartLane + ".";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public bool IsUsableAs(bool jumpRequired)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                var obstacle = entries[i].Obstacle;
                if (obstacle == null)
                    return false;

                if (jumpRequired ? !obstacle.UsableInJumpPatterns : !obstacle.UsableInNormalPatterns)
                    return false;
            }

            return true;
        }
    }
}
