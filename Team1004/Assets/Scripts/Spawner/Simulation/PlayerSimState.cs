namespace Game.Spawner
{
    public readonly struct PlayerSimState
    {
        public PlayerSimState(int lane, int lockTicks, int cooldownTicks)
        {
            Lane = lane;
            LockTicks = lockTicks;
            CooldownTicks = cooldownTicks;
        }

        public int Lane { get; }
        public int LockTicks { get; }
        public int CooldownTicks { get; }

        public bool IsAirborne(StateLayout layout)
        {
            return layout.IsAir(Lane);
        }

        public override string ToString()
        {
            return "(lane " + Lane + ", lock " + LockTicks + ", cd " + CooldownTicks + ")";
        }
    }
}
