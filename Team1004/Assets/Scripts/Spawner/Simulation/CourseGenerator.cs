using System;
using System.Collections.Generic;

namespace Game.Spawner
{
    public sealed class CourseGenerator
    {
        private readonly SimulationConfig config;
        private readonly StateLayout layout;
        private readonly List<PatternSpec> patterns = new List<PatternSpec>();
        private readonly List<PatternAnalysis> analyses = new List<PatternAnalysis>();
        private readonly DifficultyProfile profile;
        private readonly int seed;

        private readonly List<ObstacleTiming> active = new List<ObstacleTiming>();
        private readonly List<PatternPlacement> placements = new List<PatternPlacement>();
        private readonly List<int> candidateBuffer = new List<int>();
        private readonly List<float> weightBuffer = new List<float>();
        private readonly StateSet scratch;

        private DeterministicRandom random;
        private float lastBlockEnd;
        private float lastJumpRequiredBlockEnd;
        private float minArrivalTime;
        private bool hasLastBlockEnd;
        private bool hasLastJumpRequired;
        private bool lastWasJumpRequired;

        public CourseGenerator(
            SimulationConfig config,
            IReadOnlyList<PatternSpec> patterns,
            IReadOnlyList<PatternAnalysis> analyses,
            DifficultyProfile profile,
            int seed)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));

            if (patterns == null)
                throw new ArgumentNullException(nameof(patterns));

            if (analyses == null)
                throw new ArgumentNullException(nameof(analyses));

            if (patterns.Count != analyses.Count)
                throw new ArgumentException("Patterns and analyses must have the same length.", nameof(analyses));

            if (!config.Validate(out var error))
                throw new ArgumentException(error, nameof(config));

            for (var i = 0; i < patterns.Count; i++)
            {
                this.patterns.Add(patterns[i]);
                this.analyses.Add(analyses[i]);
            }

            this.seed = seed;
            layout = StateLayout.From(config);
            scratch = new StateSet(layout);
            Stats = new GenerationStats();
            BeginSection(0);
        }

        public SimulationConfig Config => config;
        public StateLayout Layout => layout;
        public int Seed => seed;
        public int SectionIndex { get; private set; }
        public float MinArrivalTime => minArrivalTime;
        public float NextDecisionTime { get; private set; }
        public bool SpawnStopped { get; private set; }
        public GenerationStats Stats { get; private set; }
        public IReadOnlyList<ObstacleTiming> ActiveObstacles => active;
        public IReadOnlyList<PatternPlacement> Placements => placements;
        public IReadOnlyList<PatternSpec> Patterns => patterns;
        public IReadOnlyList<PatternAnalysis> Analyses => analyses;

        public void BeginSection(int sectionIndex)
        {
            SectionIndex = Math.Max(0, sectionIndex);
            random = new DeterministicRandom(unchecked(seed * 7919 + SectionIndex * 104729 + 1));
            active.Clear();
            placements.Clear();
            hasLastBlockEnd = false;
            hasLastJumpRequired = false;
            lastWasJumpRequired = false;
            lastBlockEnd = 0f;
            lastJumpRequiredBlockEnd = 0f;
            minArrivalTime = config.SectionStartGrace;
            NextDecisionTime = 0f;
            SpawnStopped = false;
            Stats = new GenerationStats();
        }

        public DifficultySample SampleDifficulty(float progress)
        {
            return profile.Sample(SectionIndex, progress);
        }

        public bool TryPlaceNext(float now, float progress, StateSet frontier, out PatternPlacement placement, out RejectReason reason)
        {
            placement = null;

            if (frontier == null)
                throw new ArgumentNullException(nameof(frontier));

            var sample = profile.Sample(SectionIndex, progress);

            if (!sample.SpawnEnabled)
            {
                SpawnStopped = true;
                reason = RejectReason.SpawnDisabled;
                return false;
            }

            if (now < NextDecisionTime)
            {
                reason = RejectReason.TooEarly;
                return false;
            }

            PruneActive(now);

            var wantJump = !lastWasJumpRequired && !frontier.AllInCooldown && random.NextFloat() < sample.JumpRequiredRatio;

            if (wantJump && TrySelect(now, sample, frontier, true, out placement, out reason))
            {
                Commit(placement, sample, now);
                return true;
            }

            if (TrySelect(now, sample, frontier, false, out placement, out reason))
            {
                Commit(placement, sample, now);
                return true;
            }

            Stats.RecordEmptyDecision(reason);
            NextDecisionTime = now + config.RetryInterval;
            return false;
        }

        public float MaxEarliestArrivalDelay(float speedMultiplier)
        {
            var max = 0f;
            for (var i = 0; i < patterns.Count; i++)
            {
                if (!analyses[i].Solvable)
                    continue;

                max = Math.Max(max, PlacementBuilder.EarliestArrivalDelay(patterns[i], config, speedMultiplier));
            }

            return max;
        }

        private bool TrySelect(
            float now,
            in DifficultySample sample,
            StateSet frontier,
            bool jumpRequired,
            out PatternPlacement placement,
            out RejectReason reason)
        {
            placement = null;
            BuildCandidates(jumpRequired, sample, true);

            if (candidateBuffer.Count == 0)
                BuildCandidates(jumpRequired, sample, false);

            if (candidateBuffer.Count == 0)
            {
                reason = RejectReason.NoCandidates;
                return false;
            }

            var attempts = Math.Min(config.MaxCandidateAttempts, candidateBuffer.Count);

            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var index = DrawCandidate();
                var candidate = BuildPlacement(index, now, sample, jumpRequired);

                if (Survives(candidate, frontier, now, sample.MinReactionMargin))
                {
                    placement = candidate;
                    reason = RejectReason.None;
                    return true;
                }

                Stats.RecordRejectedCheck(patterns[index].Id);
            }

            reason = RejectReason.Unsurvivable;
            return false;
        }

        private void BuildCandidates(bool jumpRequired, in DifficultySample sample, bool useTierWeights)
        {
            candidateBuffer.Clear();
            weightBuffer.Clear();

            for (var i = 0; i < patterns.Count; i++)
            {
                var analysis = analyses[i];
                if (!analysis.Solvable || analysis.JumpRequired != jumpRequired)
                    continue;

                if (!patterns[i].IsUsableAs(jumpRequired))
                    continue;

                var weight = useTierWeights && !jumpRequired ? sample.TierWeight(analysis.Tier) : 1f;
                if (weight <= 0f)
                    continue;

                candidateBuffer.Add(i);
                weightBuffer.Add(weight);
            }
        }

        private int DrawCandidate()
        {
            var total = 0f;
            for (var i = 0; i < weightBuffer.Count; i++)
                total += weightBuffer[i];

            var roll = random.NextFloat() * total;
            var chosen = candidateBuffer.Count - 1;

            for (var i = 0; i < weightBuffer.Count; i++)
            {
                roll -= weightBuffer[i];
                if (roll < 0f)
                {
                    chosen = i;
                    break;
                }
            }

            var index = candidateBuffer[chosen];
            candidateBuffer.RemoveAt(chosen);
            weightBuffer.RemoveAt(chosen);
            return index;
        }

        private PatternPlacement BuildPlacement(int index, float now, in DifficultySample sample, bool jumpRequired)
        {
            var pattern = patterns[index];
            var speed = sample.SpeedMultiplier;
            var arrival = Math.Max(minArrivalTime, now + PlacementBuilder.EarliestArrivalDelay(pattern, config, speed));

            if (hasLastBlockEnd)
                arrival = Math.Max(arrival, lastBlockEnd + sample.PatternGap);

            if (jumpRequired && hasLastJumpRequired)
                arrival = Math.Max(arrival, lastJumpRequiredBlockEnd + config.MinJumpPatternGap);

            return PlacementBuilder.Build(pattern, config, speed, now, arrival, jumpRequired, analyses[index].Tier);
        }

        private bool Survives(PatternPlacement candidate, StateSet frontier, float now, float reactionMargin)
        {
            var nowTick = config.TimeToTick(now);
            var endTick = Math.Max(
                BlockTimeline.LastBlockTick(active, config),
                BlockTimeline.LastBlockTick(candidate.Obstacles, config)) + 2;
            endTick = Math.Max(endTick, nowTick + 1);

            var timeline = BlockTimeline.Build(active, candidate.Obstacles, config, nowTick, endTick);
            var freezeTo = config.TimeToTick(candidate.LastAppearTime) + config.DurationToTicks(reactionMargin);

            scratch.CopyFrom(frontier);
            Reachability.Propagate(scratch, nowTick, endTick, timeline, PropagationOptions.Frozen(nowTick, freezeTo, true));
            return !scratch.IsEmpty;
        }

        private void Commit(PatternPlacement placement, in DifficultySample sample, float now)
        {
            for (var i = 0; i < placement.Obstacles.Count; i++)
                active.Add(placement.Obstacles[i]);

            placements.Add(placement);

            lastBlockEnd = hasLastBlockEnd ? Math.Max(lastBlockEnd, placement.LastBlockEnd) : placement.LastBlockEnd;
            hasLastBlockEnd = true;
            lastWasJumpRequired = placement.IsJumpRequired;

            if (placement.IsJumpRequired)
            {
                lastJumpRequiredBlockEnd = placement.LastBlockEnd;
                hasLastJumpRequired = true;
            }

            Stats.RecordAccepted(placement);

            var lead = MaxEarliestArrivalDelay(sample.SpeedMultiplier);
            NextDecisionTime = Math.Max(now + config.RetryInterval, lastBlockEnd + sample.PatternGap - lead);
        }

        private void PruneActive(float now)
        {
            var threshold = now - config.SafetyPadding - 1f;

            for (var i = active.Count - 1; i >= 0; i--)
                if (active[i].BlockEnd < threshold)
                    active.RemoveAt(i);
        }
    }
}
