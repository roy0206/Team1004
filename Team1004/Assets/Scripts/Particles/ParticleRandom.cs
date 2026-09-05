namespace Game.Particles
{
    public struct ParticleRandom
    {
        private uint state;

        public ParticleRandom(uint seed)
        {
            state = seed == 0u ? 0x9E3779B9u : seed;
        }

        public uint State => state;

        public uint NextUInt()
        {
            var value = state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            state = value;
            return value;
        }

        public float NextFloat()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }
    }
}
