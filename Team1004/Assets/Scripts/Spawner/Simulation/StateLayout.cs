using System;

namespace Game.Spawner
{
    public sealed class StateLayout
    {
        public StateLayout(int laneCount, int moveTicks, int jumpTicks, int cooldownTicks)
        {
            LaneCount = Math.Max(1, laneCount);
            MoveTicks = Math.Max(0, moveTicks);
            JumpTicks = Math.Max(0, jumpTicks);
            CooldownTicks = Math.Max(0, cooldownTicks);
            LockSlots = Math.Max(MoveTicks, JumpTicks) + 1;
            CooldownSlots = CooldownTicks + 1;
            StateCount = (LaneCount + 1) * LockSlots * CooldownSlots;
        }

        public int LaneCount { get; }
        public int MoveTicks { get; }
        public int JumpTicks { get; }
        public int CooldownTicks { get; }
        public int LockSlots { get; }
        public int CooldownSlots { get; }
        public int StateCount { get; }

        public int AirLane => LaneCount;
        public int TopLane => 0;
        public int BottomLane => LaneCount - 1;
        public int MaxLockTicks => LockSlots - 1;

        public static StateLayout From(SimulationConfig config)
        {
            return new StateLayout(
                config.LaneCount,
                config.DurationToTicks(config.LaneMoveDuration),
                config.DurationToTicks(config.JumpDuration),
                config.DurationToTicks(config.JumpCooldown));
        }

        public int Index(int lane, int lockTicks, int cooldownTicks)
        {
            return (lane * LockSlots + lockTicks) * CooldownSlots + cooldownTicks;
        }

        public PlayerSimState Decode(int index)
        {
            var cooldown = index % CooldownSlots;
            var rest = index / CooldownSlots;
            var lockTicks = rest % LockSlots;
            var lane = rest / LockSlots;
            return new PlayerSimState(lane, lockTicks, cooldown);
        }

        public bool IsAir(int lane)
        {
            return lane == AirLane;
        }
    }
}
