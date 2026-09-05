using System.Collections.Generic;
using UnityEngine;

namespace Game.Boss
{
    [CreateAssetMenu(fileName = "WaterfallBossData", menuName = "Team1004/Boss/Waterfall Boss Data")]
    public sealed class WaterfallBossData : BossData
    {
        [SerializeField] private float waterfallX = 5.5f;
        [SerializeField] private float finalWaterfallEnterX = 9.5f;
        [SerializeField] private float finalWaterfallEnterDuration = 1f;
        [SerializeField] private float spawnX = 7.5f;
        [SerializeField] private float exitX = -8f;
        [SerializeField] private float rockTrail = 1f;
        [SerializeField] private float safeWindowDuration = 2f;
        [SerializeField] private float jumpCuePulseDuration = 0.35f;

        public float WaterfallX => waterfallX;
        public float FinalWaterfallEnterX => finalWaterfallEnterX;
        public float FinalWaterfallEnterDuration => finalWaterfallEnterDuration;
        public float SpawnX => spawnX;
        public float ExitX => exitX;
        public float RockTrail => rockTrail;
        public float SafeWindowDuration => safeWindowDuration;
        public float JumpCuePulseDuration => jumpCuePulseDuration;

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
