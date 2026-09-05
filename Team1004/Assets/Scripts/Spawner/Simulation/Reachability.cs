using System;

namespace Game.Spawner
{
    public static class Reachability
    {
        [ThreadStatic] private static int[] scratch;

        public static void Propagate(
            StateSet set,
            int fromTick,
            int toTick,
            BlockTimeline timeline,
            in PropagationOptions options)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));

            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));

            var layout = set.Layout;
            var current = set.Costs;
            var next = GetScratch(layout.StateCount);
            var moveLock = Math.Max(0, layout.MoveTicks - 1);
            var jumpLock = layout.JumpTicks - 1;

            for (var tick = fromTick; tick < toTick; tick++)
            {
                var mask = timeline.MaskAt(tick);
                var frozen = options.IsFrozen(tick) || !options.AllowInputs;
                Array.Fill(next, -1);

                for (var index = 0; index < current.Length; index++)
                {
                    var inputs = current[index];
                    if (inputs < 0)
                        continue;

                    var cooldown = index % layout.CooldownSlots;
                    var rest = index / layout.CooldownSlots;
                    var lockTicks = rest % layout.LockSlots;
                    var lane = rest / layout.LockSlots;
                    var airborne = lane == layout.AirLane;

                    if (lockTicks == 0 && !airborne && (mask & (1 << lane)) != 0)
                        continue;

                    var cooldownNext = cooldown > 0 ? cooldown - 1 : 0;

                    if (airborne)
                    {
                        if (lockTicks <= 1)
                            Relax(next, layout, layout.TopLane, 0, layout.CooldownTicks, inputs);
                        else
                            Relax(next, layout, lane, lockTicks - 1, 0, inputs);

                        continue;
                    }

                    if (lockTicks > 0)
                    {
                        Relax(next, layout, lane, lockTicks - 1, cooldownNext, inputs);
                        continue;
                    }

                    Relax(next, layout, lane, 0, cooldownNext, inputs);

                    if (frozen)
                        continue;

                    if (lane > 0)
                        Relax(next, layout, lane - 1, moveLock, cooldownNext, inputs + 1);

                    if (lane < layout.LaneCount - 1)
                        Relax(next, layout, lane + 1, moveLock, cooldownNext, inputs + 1);

                    if (lane == layout.TopLane && cooldown == 0 && options.AllowJump)
                    {
                        if (jumpLock <= 0)
                            Relax(next, layout, layout.TopLane, 0, layout.CooldownTicks, inputs + 1);
                        else
                            Relax(next, layout, layout.AirLane, jumpLock, 0, inputs + 1);
                    }
                }

                Array.Copy(next, current, current.Length);
            }

            Prune(set, toTick, timeline);
        }

        public static void Prune(StateSet set, int tick, BlockTimeline timeline)
        {
            var layout = set.Layout;
            var costs = set.Costs;
            var mask = timeline.MaskAt(tick);

            if (mask == 0)
                return;

            for (var index = 0; index < costs.Length; index++)
            {
                if (costs[index] < 0)
                    continue;

                var rest = index / layout.CooldownSlots;
                var lockTicks = rest % layout.LockSlots;
                var lane = rest / layout.LockSlots;

                if (lockTicks == 0 && lane != layout.AirLane && (mask & (1 << lane)) != 0)
                    costs[index] = -1;
            }
        }

        private static void Relax(int[] next, StateLayout layout, int lane, int lockTicks, int cooldown, int inputs)
        {
            var index = layout.Index(lane, lockTicks, cooldown);
            if (next[index] < 0 || next[index] > inputs)
                next[index] = inputs;
        }

        private static int[] GetScratch(int size)
        {
            if (scratch == null || scratch.Length != size)
                scratch = new int[size];

            return scratch;
        }
    }
}
