using System.Collections.Generic;
using UnityEngine;

namespace Game.Boss
{
    [CreateAssetMenu(fileName = "FishingLineBossData", menuName = "Team1004/Boss/Fishing Line Boss Data")]
    public sealed class FishingLineBossData : BossData
    {
        [SerializeField] private float hookParkOffset = 1.5f;
        [SerializeField] private float hookDescendRatio = 0.4f;

        public float HookParkOffset => hookParkOffset;
        public float HookDescendRatio => Mathf.Clamp01(hookDescendRatio);

        protected override string DefaultDisplayName => "낚싯줄";

        protected override IEnumerable<BossPattern> CreateDefaultPatterns()
        {
            yield return new BossPattern("Top", BossLanes.Mask(0));
            yield return new BossPattern("Middle", BossLanes.Mask(1));
            yield return new BossPattern("Bottom", BossLanes.Mask(2));
        }
    }
}
