using System.Collections.Generic;
using UnityEngine;

namespace Game.Boss
{
    [CreateAssetMenu(fileName = "WaterfallBossData", menuName = "Team1004/Boss/Waterfall Boss Data")]
    public sealed class WaterfallBossData : BossData
    {
        [SerializeField] private float waterfallX = 5.5f;
        [SerializeField] private float finalWaterfallEnterX = 9.5f;
        [SerializeField] private float finalApproachDuration = 3f;
        [SerializeField] private float finalClearHeight = 2.2f;
        [SerializeField] private float finalContactHalfWidth = 0.75f;
        [SerializeField] private float finalPassDuration = 0.8f;
        [SerializeField] private float spawnX = 7.5f;
        [SerializeField] private float exitX = -8f;
        [SerializeField] private float rockTrail = 1f;

        public float WaterfallX => waterfallX;
        public float FinalWaterfallEnterX => finalWaterfallEnterX;
        public float FinalApproachDuration => Mathf.Max(0.1f, finalApproachDuration);
        public float FinalClearHeight => finalClearHeight;
        public float FinalContactHalfWidth => Mathf.Max(0f, finalContactHalfWidth);
        public float FinalPassDuration => Mathf.Max(0.1f, finalPassDuration);
        public float SpawnX => spawnX;
        public float ExitX => exitX;
        public float RockTrail => rockTrail;

        protected override string DefaultDisplayName => "폭포";

        protected override IEnumerable<BossPattern> CreateDefaultPatterns()
        {
            yield return new BossPattern("Top", BossLanes.Mask(0));
            yield return new BossPattern("Middle", BossLanes.Mask(1));
            yield return new BossPattern("Bottom", BossLanes.Mask(2));
            yield return new BossPattern("TopMiddle", BossLanes.Mask(0, 1));
            yield return new BossPattern("MiddleBottom", BossLanes.Mask(1, 2));
            yield return new BossPattern("All", BossLanes.All(3), 1f, true);
        }
    }
}
