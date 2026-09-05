using System;
using Game.Config;
using Game.Player;

namespace Game.Spawner
{
    public readonly struct SpawnPlayerState
    {
        public SpawnPlayerState(int lane, bool isAirborne, float lockRemaining, float jumpCooldownRemaining)
        {
            Lane = lane;
            IsAirborne = isAirborne;
            LockRemaining = Math.Max(0f, lockRemaining);
            JumpCooldownRemaining = Math.Max(0f, jumpCooldownRemaining);
        }

        public int Lane { get; }
        public bool IsAirborne { get; }
        public float LockRemaining { get; }
        public float JumpCooldownRemaining { get; }

        public bool IsMoving => !IsAirborne && LockRemaining > 0f;

        public static SpawnPlayerState Grounded(int lane, float moveRemaining, float jumpCooldownRemaining)
        {
            return new SpawnPlayerState(lane, false, moveRemaining, jumpCooldownRemaining);
        }

        public static SpawnPlayerState Airborne(float airborneRemaining, float jumpCooldownRemaining)
        {
            return new SpawnPlayerState(0, true, airborneRemaining, jumpCooldownRemaining);
        }

        public static SpawnPlayerState FromPlayer(
            LanePlayer player,
            float jumpCooldownRemaining,
            float airborneRemaining = -1f,
            float moveRemaining = -1f)
        {
            var config = GameConfig.Current;

            if (player == null)
                return Grounded(config.LaneCount / 2, 0f, jumpCooldownRemaining);

            if (player.IsAirborne)
                return Airborne(airborneRemaining >= 0f ? airborneRemaining : config.JumpDuration, jumpCooldownRemaining);

            var move = 0f;
            if (player.IsMoving)
                move = moveRemaining >= 0f ? moveRemaining : config.LaneMoveDuration;

            return Grounded(player.CurrentLane, move, jumpCooldownRemaining);
        }

        public PlayerSimState ToSimState(StateLayout layout, SimulationConfig config)
        {
            var cooldownTicks = Math.Min(layout.CooldownTicks, config.DurationToTicks(JumpCooldownRemaining));

            if (IsAirborne)
            {
                var airLock = Math.Clamp(config.DurationToTicks(LockRemaining), 1, layout.MaxLockTicks);
                return new PlayerSimState(layout.AirLane, airLock, 0);
            }

            var lane = Math.Clamp(Lane, 0, layout.BottomLane);
            var lockTicks = Math.Min(layout.MaxLockTicks, config.DurationToTicks(LockRemaining));
            return new PlayerSimState(lane, lockTicks, cooldownTicks);
        }
    }
}
