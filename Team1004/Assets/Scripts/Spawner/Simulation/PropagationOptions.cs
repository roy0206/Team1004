namespace Game.Spawner
{
    public readonly struct PropagationOptions
    {
        public PropagationOptions(int freezeFromTick, int freezeToTick, bool allowJump, bool allowInputs)
        {
            FreezeFromTick = freezeFromTick;
            FreezeToTick = freezeToTick;
            AllowJump = allowJump;
            AllowInputs = allowInputs;
        }

        public int FreezeFromTick { get; }
        public int FreezeToTick { get; }
        public bool AllowJump { get; }
        public bool AllowInputs { get; }

        public static PropagationOptions Free => new PropagationOptions(0, 0, true, true);

        public static PropagationOptions NoJump => new PropagationOptions(0, 0, false, true);

        public static PropagationOptions Frozen(int fromTick, int toTick, bool allowJump)
        {
            return new PropagationOptions(fromTick, toTick, allowJump, true);
        }

        public bool IsFrozen(int tick)
        {
            return tick >= FreezeFromTick && tick < FreezeToTick;
        }
    }
}
