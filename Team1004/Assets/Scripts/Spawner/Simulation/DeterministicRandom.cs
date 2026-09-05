namespace Game.Spawner
{
    public sealed class DeterministicRandom
    {
        private uint state;

        public DeterministicRandom(int seed)
        {
            state = unchecked((uint)seed * 2654435761u) ^ 0x9E3779B9u;
            if (state == 0)
                state = 0x1234567u;
        }

        public uint NextUInt()
        {
            var x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        public float NextFloat()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 1)
                return 0;

            return (int)(NextUInt() % (uint)maxExclusive);
        }
    }
}
