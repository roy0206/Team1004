namespace Game.Boss
{
    public readonly struct BossAttackTiming
    {
        public BossAttackTiming(float telegraph, float attack, float recovery)
            : this(telegraph, attack, recovery, telegraph)
        {
        }

        public BossAttackTiming(float telegraph, float attack, float recovery, float fullLaneTelegraph)
        {
            Telegraph = telegraph < 0f ? 0f : telegraph;
            Attack = attack < 0f ? 0f : attack;
            Recovery = recovery < 0f ? 0f : recovery;
            FullLaneTelegraph = fullLaneTelegraph < 0f ? 0f : fullLaneTelegraph;
        }

        public static BossAttackTiming Default => new(0.8f, 0.6f, 0.6f, 1f);

        public float Telegraph { get; }
        public float Attack { get; }
        public float Recovery { get; }
        public float FullLaneTelegraph { get; }
        public float Cycle => Telegraph + Attack + Recovery;
    }
}
