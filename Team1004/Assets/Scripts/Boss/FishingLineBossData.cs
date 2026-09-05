using System.Collections.Generic;
using UnityEngine;

namespace Game.Boss
{
    [CreateAssetMenu(fileName = "FishingLineBossData", menuName = "Team1004/Boss/Fishing Line Boss Data")]
    public sealed class FishingLineBossData : BossData
    {
        [SerializeField] private float hookParkOffset = 1.5f;
        [SerializeField] private float hookEnterX = 6f;
        [SerializeField] private float hookExitX = -7.5f;
        [SerializeField] private float hookSweepDuration = 1.5f;
        [SerializeField] private float hookBobAmplitude = 0.12f;
        [SerializeField] private float hookBobFrequency = 1.8f;
        [SerializeField] private float lineAnchorOffset = 1.2f;
        [SerializeField] private float lineDragOffset = 0.9f;
        [SerializeField] private float lineWidth = 0.06f;

        public float HookParkOffset => hookParkOffset;
        public float HookEnterX => hookEnterX;
        public float HookExitX => hookExitX;
        public float HookSweepDuration => Mathf.Max(0.01f, hookSweepDuration);
        public float HookBobAmplitude => Mathf.Max(0f, hookBobAmplitude);
        public float HookBobFrequency => Mathf.Max(0f, hookBobFrequency);
        public float LineAnchorOffset => lineAnchorOffset;
        public float LineDragOffset => lineDragOffset;
        public float LineWidth => Mathf.Max(0.001f, lineWidth);

        protected override string DefaultDisplayName => "낚싯줄";

        public float GetPlayerCrossRatio(float playerX)
        {
            var span = hookEnterX - hookExitX;

            if (Mathf.Abs(span) < 0.001f)
                return 1f;

            return Mathf.Clamp01((hookEnterX - playerX) / span);
        }

        protected override IEnumerable<BossPattern> CreateDefaultPatterns()
        {
            yield return new BossPattern("Top", BossLanes.Mask(0));
            yield return new BossPattern("Middle", BossLanes.Mask(1));
            yield return new BossPattern("Bottom", BossLanes.Mask(2));
        }
    }
}
