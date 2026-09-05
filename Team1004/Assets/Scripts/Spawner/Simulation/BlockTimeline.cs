using System;
using System.Collections.Generic;

namespace Game.Spawner
{
    public sealed class BlockTimeline
    {
        private readonly int[] masks;

        private BlockTimeline(int startTick, int endTick)
        {
            StartTick = startTick;
            EndTick = endTick;
            masks = new int[endTick - startTick + 1];
        }

        public int StartTick { get; }
        public int EndTick { get; }

        public static BlockTimeline Build(
            IReadOnlyList<ObstacleTiming> first,
            IReadOnlyList<ObstacleTiming> second,
            SimulationConfig config,
            int startTick,
            int endTick)
        {
            var timeline = new BlockTimeline(startTick, Math.Max(startTick, endTick));
            timeline.AddAll(first, config);
            timeline.AddAll(second, config);
            return timeline;
        }

        public static void BlockRange(ObstacleTiming obstacle, SimulationConfig config, out int startTick, out int endTick)
        {
            startTick = (int)Math.Floor((obstacle.BlockStart - config.SafetyPadding) / config.TickSeconds + 0.0001f);
            endTick = (int)Math.Ceiling((obstacle.BlockEnd + config.SafetyPadding) / config.TickSeconds - 0.0001f);
        }

        public static int LastBlockTick(IReadOnlyList<ObstacleTiming> obstacles, SimulationConfig config)
        {
            var last = -1;
            if (obstacles == null)
                return last;

            for (var i = 0; i < obstacles.Count; i++)
            {
                BlockRange(obstacles[i], config, out _, out var end);
                last = Math.Max(last, end);
            }

            return last;
        }

        public int MaskAt(int tick)
        {
            if (tick < StartTick || tick > EndTick)
                return 0;

            return masks[tick - StartTick];
        }

        public bool IsBlocked(int lane, int tick)
        {
            return LaneMask.Contains(MaskAt(tick), lane);
        }

        private void AddAll(IReadOnlyList<ObstacleTiming> obstacles, SimulationConfig config)
        {
            if (obstacles == null)
                return;

            for (var i = 0; i < obstacles.Count; i++)
            {
                BlockRange(obstacles[i], config, out var start, out var end);
                start = Math.Max(start, StartTick);
                end = Math.Min(end, EndTick);

                for (var tick = start; tick <= end; tick++)
                    masks[tick - StartTick] |= obstacles[i].LaneMask;
            }
        }
    }
}
