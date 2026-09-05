using System;

namespace Game.Ledge
{
    [Flags]
    public enum LedgeSignal
    {
        None = 0,
        Spawned = 1 << 0,
        Approaching = 1 << 1,
        Blocked = 1 << 2,
        Resumed = 1 << 3,
        Cleared = 1 << 4,
        Finished = 1 << 5,
        Retired = 1 << 6,
        Failed = 1 << 7
    }
}
