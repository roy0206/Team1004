using System;
using System.Collections.Generic;

namespace Game.Boss
{
    public sealed class BossPatternSelector
    {
        private readonly IReadOnlyList<BossPattern> patterns;
        private readonly Random random;
        private readonly bool allowRepeat;
        private readonly List<BossPattern> candidates = new();

        public BossPatternSelector(IReadOnlyList<BossPattern> patterns, int seed, bool allowRepeat)
        {
            this.patterns = patterns ?? throw new ArgumentNullException(nameof(patterns));
            this.allowRepeat = allowRepeat;
            random = seed != 0 ? new Random(seed) : new Random();
        }

        public int Count => patterns.Count;
        public bool AllowRepeat => allowRepeat;
        public BossPattern Last { get; private set; }

        public bool TryPick(out BossPattern pattern)
        {
            return TryPick(null, out pattern);
        }

        public bool TryPick(Func<BossPattern, bool> filter, out BossPattern pattern)
        {
            var total = Collect(filter, !allowRepeat);

            if (candidates.Count == 0 && !allowRepeat)
                total = Collect(filter, false);

            if (candidates.Count == 0)
            {
                pattern = null;
                return false;
            }

            var roll = random.NextDouble() * total;
            var picked = candidates[candidates.Count - 1];

            for (var i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].Weight;

                if (roll < 0d)
                {
                    picked = candidates[i];
                    break;
                }
            }

            Last = picked;
            pattern = picked;
            candidates.Clear();
            return true;
        }

        public void ResetHistory()
        {
            Last = null;
        }

        private double Collect(Func<BossPattern, bool> filter, bool excludeLast)
        {
            candidates.Clear();
            var total = 0d;

            for (var i = 0; i < patterns.Count; i++)
            {
                var candidate = patterns[i];

                if (candidate == null || candidate.Weight <= 0f)
                    continue;

                if (excludeLast && ReferenceEquals(candidate, Last))
                    continue;

                if (filter != null && !filter(candidate))
                    continue;

                candidates.Add(candidate);
                total += candidate.Weight;
            }

            return total;
        }
    }
}
