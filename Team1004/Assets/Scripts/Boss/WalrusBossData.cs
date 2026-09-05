using System.Collections.Generic;
using UnityEngine;

namespace Game.Boss
{
    [CreateAssetMenu(fileName = "WalrusBossData", menuName = "Team1004/Boss/Walrus Boss Data")]
    public sealed class WalrusBossData : BossData
    {
        [SerializeField] private float restX = 3f;
        [SerializeField] private float dashX = -7f;
        [SerializeField] private float exitX = 9f;
        [SerializeField] private float bodyWidth = 1.6f;
        [SerializeField] private float singleBodyHeight = 0.9f;
        [SerializeField] private float doubleBodyHeight = 2f;
        [SerializeField] private float approachRatio = 0.6f;

        public float RestX => restX;
        public float DashX => dashX;
        public float ExitX => exitX;
        public float BodyWidth => bodyWidth;
        public float SingleBodyHeight => singleBodyHeight;
        public float DoubleBodyHeight => doubleBodyHeight;
        public float ApproachRatio => Mathf.Clamp01(approachRatio);

        protected override string DefaultDisplayName => "바다코끼리";

        protected override IEnumerable<BossPattern> CreateDefaultPatterns()
        {
            yield return new BossPattern("MouthTop", BossLanes.Mask(0));
            yield return new BossPattern("MouthMiddle", BossLanes.Mask(1));
            yield return new BossPattern("MouthBottom", BossLanes.Mask(2));
            yield return new BossPattern("ArmsTopMiddle", BossLanes.Mask(0, 1));
            yield return new BossPattern("ArmsMiddleBottom", BossLanes.Mask(1, 2));
        }
    }
}
