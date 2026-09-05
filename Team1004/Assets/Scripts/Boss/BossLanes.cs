namespace Game.Boss
{
    public static class BossLanes
    {
        public const int MaxLanes = 30;

        public static int Mask(int lane)
        {
            return lane < 0 || lane >= MaxLanes ? 0 : 1 << lane;
        }

        public static int Mask(int laneA, int laneB)
        {
            return Mask(laneA) | Mask(laneB);
        }

        public static int All(int laneCount)
        {
            if (laneCount <= 0)
                return 0;

            if (laneCount >= MaxLanes)
                laneCount = MaxLanes;

            return (1 << laneCount) - 1;
        }

        public static bool Contains(int mask, int lane)
        {
            return (mask & Mask(lane)) != 0;
        }

        public static int Count(int mask)
        {
            var count = 0;

            for (var lane = 0; lane < MaxLanes; lane++)
                if ((mask & (1 << lane)) != 0)
                    count++;

            return count;
        }

        public static int First(int mask)
        {
            for (var lane = 0; lane < MaxLanes; lane++)
                if ((mask & (1 << lane)) != 0)
                    return lane;

            return -1;
        }

        public static int Last(int mask)
        {
            for (var lane = MaxLanes - 1; lane >= 0; lane--)
                if ((mask & (1 << lane)) != 0)
                    return lane;

            return -1;
        }
    }
}
