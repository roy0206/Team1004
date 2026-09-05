using System.Collections.Generic;
using Game.Config;
using UnityEngine;

namespace Game.Boss
{
    public abstract class BossData : ScriptableObject
    {
        public const float DefaultDuration = 30f;

        [SerializeField] private string displayName;
        [SerializeField] private bool useGameConfigTiming = true;
        [SerializeField] private float duration = DefaultDuration;
        [SerializeField] private float telegraphDuration = 1.2f;
        [SerializeField] private float attackDuration = 0.6f;
        [SerializeField] private float recoveryDuration = 0.6f;
        [SerializeField] private float fullLaneTelegraphDuration = 1.5f;
        [SerializeField] private float telegraphImminentLead = 0.2f;
        [SerializeField] private float introDuration = 1f;
        [SerializeField] private float outroDuration = 0.8f;
        [SerializeField] private bool allowRepeatPattern;
        [SerializeField] private bool holdsWorld;
        [SerializeField] private int seed;
        [SerializeField] private List<BossPattern> patterns = new();

        public string DisplayName => string.IsNullOrEmpty(displayName) ? DefaultDisplayName : displayName;
        public bool UseGameConfigTiming => useGameConfigTiming;
        public bool HoldsWorld => holdsWorld;
        public float Duration => useGameConfigTiming ? GameConfig.Current.BossDuration : duration;
        public float TelegraphDuration => useGameConfigTiming ? GameConfig.Current.BossTelegraphDuration : telegraphDuration;
        public float AttackDuration => useGameConfigTiming ? GameConfig.Current.BossAttackDuration : attackDuration;
        public float RecoveryDuration => useGameConfigTiming ? GameConfig.Current.BossRecoveryDuration : recoveryDuration;
        public float FullLaneTelegraphDuration =>
            useGameConfigTiming ? GameConfig.Current.BossFullLaneTelegraphDuration : fullLaneTelegraphDuration;
        public float TelegraphImminentLead => telegraphImminentLead;
        public float IntroDuration => introDuration;
        public float OutroDuration => outroDuration;
        public bool AllowRepeatPattern => allowRepeatPattern;
        public int Seed => seed;
        public IReadOnlyList<BossPattern> Patterns => patterns;
        public BossAttackTiming Timing => new(TelegraphDuration, AttackDuration, RecoveryDuration, FullLaneTelegraphDuration);

        protected virtual string DefaultDisplayName => "Boss";

        public void EnsureDefaultPatterns()
        {
            if (patterns.Count > 0)
                return;

            patterns.AddRange(CreateDefaultPatterns());
        }

        protected abstract IEnumerable<BossPattern> CreateDefaultPatterns();
    }
}
