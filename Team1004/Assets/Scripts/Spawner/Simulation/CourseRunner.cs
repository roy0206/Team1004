using System;
using System.Collections.Generic;

namespace Game.Spawner
{
    public sealed class CourseRunner
    {
        private readonly SimulationConfig config;
        private readonly IReadOnlyList<PatternSpec> patterns;
        private readonly IReadOnlyList<PatternAnalysis> analyses;
        private readonly DifficultyProfile profile;

        public CourseRunner(
            SimulationConfig config,
            IReadOnlyList<PatternSpec> patterns,
            IReadOnlyList<PatternAnalysis> analyses,
            DifficultyProfile profile)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.patterns = patterns ?? throw new ArgumentNullException(nameof(patterns));
            this.analyses = analyses ?? throw new ArgumentNullException(nameof(analyses));
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public CourseResult RunSeconds(int sectionIndex, float durationSeconds, int seed, float stepSeconds = 0.1f)
        {
            return Run(sectionIndex, Math.Max(0f, durationSeconds) * config.ScrollSpeed, seed, stepSeconds);
        }

        public CourseResult Run(int sectionIndex, float sectionLength, int seed, float stepSeconds = 0.1f)
        {
            if (stepSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(stepSeconds));

            var generator = new CourseGenerator(config, patterns, analyses, profile, seed);
            generator.BeginSection(sectionIndex);

            var layout = generator.Layout;
            var frontier = new StateSet(layout);
            frontier.Add(layout.LaneCount / 2, 0, 0);

            var duration = Math.Max(0f, sectionLength) / config.ScrollSpeed;
            var lastTick = 0;
            var survivable = true;
            var steps = (int)Math.Ceiling(duration / stepSeconds);

            for (var step = 0; step <= steps; step++)
            {
                var time = Math.Min(duration, step * stepSeconds);
                var tick = config.TimeToTick(time);

                if (tick > lastTick)
                {
                    PropagateFrontier(frontier, lastTick, tick, generator.ActiveObstacles);
                    lastTick = tick;

                    if (frontier.IsEmpty)
                    {
                        survivable = false;
                        break;
                    }
                }

                var progress = duration > 0f ? time / duration : 1f;

                if (!generator.SpawnStopped && time >= generator.NextDecisionTime)
                    generator.TryPlaceNext(time, progress, frontier, out _, out _);
            }

            if (survivable)
            {
                var endTick = Math.Max(lastTick, BlockTimeline.LastBlockTick(generator.ActiveObstacles, config) + 2);
                PropagateFrontier(frontier, lastTick, endTick, generator.ActiveObstacles);
                survivable = !frontier.IsEmpty;
            }

            return new CourseResult(
                sectionIndex,
                seed,
                duration,
                new List<PatternPlacement>(generator.Placements),
                generator.Stats,
                survivable,
                frontier.Count,
                frontier.MinCost);
        }

        private void PropagateFrontier(StateSet frontier, int fromTick, int toTick, IReadOnlyList<ObstacleTiming> obstacles)
        {
            var timeline = BlockTimeline.Build(obstacles, null, config, fromTick, toTick);
            Reachability.Propagate(frontier, fromTick, toTick, timeline, PropagationOptions.Free);
        }
    }
}
