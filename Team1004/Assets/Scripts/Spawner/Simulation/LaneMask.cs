namespace Game.Spawner
{
    public static class LaneMask
    {
        public static int Of(int lane)
        {
            return 1 << lane;
        }

        public static int Span(int startLane, int laneSpan)
        {
            var mask = 0;
            for (var i = 0; i < laneSpan; i++)
                mask |= 1 << (startLane + i);

            return mask;
        }

        public static int All(int laneCount)
        {
            return (1 << laneCount) - 1;
        }

        public static bool Contains(int mask, int lane)
        {
            return (mask & (1 << lane)) != 0;
        }
    }
}
