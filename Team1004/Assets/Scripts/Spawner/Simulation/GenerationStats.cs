using System.Collections.Generic;

namespace Game.Spawner
{
    public sealed class GenerationStats
    {
        private readonly Dictionary<string, int> acceptedByPattern = new Dictionary<string, int>();
        private readonly Dictionary<string, int> rejectedByPattern = new Dictionary<string, int>();

        public int Accepted { get; private set; }
        public int JumpRequiredAccepted { get; private set; }
        public int RejectedChecks { get; private set; }
        public int EmptyDecisions { get; private set; }
        public int NoCandidateDecisions { get; private set; }

        public IReadOnlyDictionary<string, int> AcceptedByPattern => acceptedByPattern;
        public IReadOnlyDictionary<string, int> RejectedByPattern => rejectedByPattern;

        public void RecordAccepted(PatternPlacement placement)
        {
            Accepted++;
            if (placement.IsJumpRequired)
                JumpRequiredAccepted++;

            Increment(acceptedByPattern, placement.Pattern.Id);
        }

        public void RecordRejectedCheck(string patternId)
        {
            RejectedChecks++;
            Increment(rejectedByPattern, patternId);
        }

        public void RecordEmptyDecision(RejectReason reason)
        {
            EmptyDecisions++;
            if (reason == RejectReason.NoCandidates)
                NoCandidateDecisions++;
        }

        public void Merge(GenerationStats other)
        {
            Accepted += other.Accepted;
            JumpRequiredAccepted += other.JumpRequiredAccepted;
            RejectedChecks += other.RejectedChecks;
            EmptyDecisions += other.EmptyDecisions;
            NoCandidateDecisions += other.NoCandidateDecisions;

            foreach (var pair in other.acceptedByPattern)
                Increment(acceptedByPattern, pair.Key, pair.Value);

            foreach (var pair in other.rejectedByPattern)
                Increment(rejectedByPattern, pair.Key, pair.Value);
        }

        private static void Increment(Dictionary<string, int> map, string key, int amount = 1)
        {
            map.TryGetValue(key, out var value);
            map[key] = value + amount;
        }
    }
}
