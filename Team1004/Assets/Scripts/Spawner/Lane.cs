using System;

namespace Game.Spawner
{
    public enum Lane
    {
        Top = 0,
        Middle = 1,
        Bottom = 2
    }

    [Flags]
    public enum LaneFlags
    {
        None = 0,
        Top = 1 << 0,
        Middle = 1 << 1,
        Bottom = 1 << 2,
        All = Top | Middle | Bottom
    }
}
