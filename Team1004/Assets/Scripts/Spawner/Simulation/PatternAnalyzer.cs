using System;
using System.Collections.Generic;

namespace Game.Spawner
{
    public static class PatternAnalyzer
    {
        private readonly struct ModeResult
        {
            public ModeResult(bool allSolvable, int worstInputs, int marginTicks)
            {
                AllSolvable = allSolvable;
                WorstInputs = worstInputs;
                MarginTicks = marginTicks;
            }

            public bool AllSolvable { get; }
            public int WorstInputs { get; }
            public int MarginTicks { get; }
        }

        public static PatternAnalysis Analyze(PatternSpec pattern, SimulationConfig config, float speedMultiplier)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!config.Validate(out var configError))
                return PatternAnalysis.Invalid(pattern, speedMultiplier, configError);

            if (!pattern.Validate(config.LaneCount, out var error))
                return PatternAnalysis.Invalid(pattern, speedMultiplier, error);

            var earliest = PlacementBuilder.EarliestArrivalDelay(pattern, config, speedMultiplier);
            var placement = PlacementBuilder.Build(pattern, config, speedMultiplier, 0f, earliest, false, 0);
            var layout = StateLayout.From(config);
            var endTick = BlockTimeline.LastBlockTick(placement.Obstacles, config) + 2;
            var timeline = BlockTimeline.Build(placement.Obstacles, null, config, 0, endTick);
            var lastAppearTick = Math.Max(0, config.TimeToTick(placement.LastAppearTime));

            var noJump = Evaluate(layout, timeline, endTick, lastAppearTick, false, layout.CooldownTicks);
            var withJump = Evaluate(layout, timeline, endTick, lastAppearTick, true, 0);

            var jumpRequired = !noJump.AllSolvable && withJump.AllSolvable;
            var chosen = jumpRequired ? withJump : noJump;
            var margin = chosen.MarginTicks < 0 ? -1f : chosen.MarginTicks * config.TickSeconds;

            return new PatternAnalysis(
                pattern,
                speedMultiplier,
                null,
                noJump.AllSolvable,
                withJump.AllSolvable,
                chosen.WorstInputs,
                margin,
                earliest,
                placement.LastBlockEnd - placement.FirstBlockStart);
        }

        public static List<PatternAnalysis> AnalyzeAll(IReadOnlyList<PatternSpec> patterns, SimulationConfig config, float speedMultiplier)
        {
            var result = new List<PatternAnalysis>(patterns.Count);
            for (var i = 0; i < patterns.Count; i++)
                result.Add(Analyze(patterns[i], config, speedMultiplier));

            return result;
        }

        private static ModeResult Evaluate(
            StateLayout layout,
            BlockTimeline timeline,
            int endTick,
            int lastAppearTick,
            bool allowJump,
            int startCooldown)
        {
            var set = new StateSet(layout);
            var allSolvable = true;
            var worstInputs = 0;
            var marginTicks = int.MaxValue;
            var lockOptions = layout.MoveTicks > 1 ? new[] { 0, layout.MoveTicks - 1 } : new[] { 0 };

            for (var lane = 0; lane < layout.LaneCount; lane++)
            {
                for (var l = 0; l < lockOptions.Length; l++)
                {
                    var lockTicks = lockOptions[l];
                    set.Clear();
                    set.Add(lane, lockTicks, startCooldown);
                    Reachability.Propagate(set, 0, endTick, timeline, new PropagationOptions(0, 0, allowJump, true));

                    if (set.IsEmpty)
                    {
                        allSolvable = false;
                        marginTicks = -1;
                        continue;
                    }

                    worstInputs = Math.Max(worstInputs, set.MinCost);

                    if (marginTicks < 0)
                        continue;

                    var margin = FindMargin(set, lane, lockTicks, startCooldown, timeline, endTick, lastAppearTick, allowJump);
                    marginTicks = Math.Min(marginTicks, margin);
                }
            }

            if (marginTicks == int.MaxValue)
                marginTicks = -1;

            return new ModeResult(allSolvable, worstInputs, marginTicks);
        }

        private static int FindMargin(
            StateSet set,
            int lane,
            int lockTicks,
            int cooldown,
            BlockTimeline timeline,
            int endTick,
            int lastAppearTick,
            bool allowJump)
        {
            var maxMargin = Math.Max(0, endTick - lastAppearTick);

            if (!Survives(set, lane, lockTicks, cooldown, timeline, endTick, lastAppearTick, 0, allowJump))
                return -1;

            if (Survives(set, lane, lockTicks, cooldown, timeline, endTick, lastAppearTick, maxMargin, allowJump))
                return maxMargin;

            var low = 0;
            var high = maxMargin;

            while (high - low > 1)
            {
                var mid = (low + high) / 2;
                if (Survives(set, lane, lockTicks, cooldown, timeline, endTick, lastAppearTick, mid, allowJump))
                    low = mid;
                else
                    high = mid;
            }

            return low;
        }

        private static bool Survives(
            StateSet set,
            int lane,
            int lockTicks,
            int cooldown,
            BlockTimeline timeline,
            int endTick,
            int lastAppearTick,
            int marginTicks,
            bool allowJump)
        {
            set.Clear();
            set.Add(lane, lockTicks, cooldown);
            Reachability.Propagate(set, 0, endTick, timeline, PropagationOptions.Frozen(0, lastAppearTick + marginTicks, allowJump));
            return !set.IsEmpty;
        }
    }
}
