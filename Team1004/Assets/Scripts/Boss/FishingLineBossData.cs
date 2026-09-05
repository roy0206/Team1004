using System.Collections.Generic;
using UnityEngine;

namespace Game.Boss
{
    [CreateAssetMenu(fileName = "FishingLineBossData", menuName = "Team1004/Boss/Fishing Line Boss Data")]
    public sealed class FishingLineBossData : BossData
    {
        [SerializeField] private float boatX = 2.5f;
        [SerializeField] private float boatEnterOffsetX = 6f;
        [SerializeField] private float boatEnterDuration = 0.8f;
        [SerializeField] private float boatFloatOffset = 0.623f;
        [SerializeField] private float boatTiltScale = 1f;
        [SerializeField] private float boatSlopeSpan = 0.8f;
        [SerializeField] private float boatFloatSmoothing = 12f;
        [SerializeField] private Vector2 rodTipOffset = new(-1.825f, 2.06f);
        [SerializeField] private float lineAnchorOffset = 1.2f;
        [SerializeField] private float hookScale = 0.55f;
        [SerializeField] private float hookRestOffset = 0.3f;
        [SerializeField] private float castDuration = 0.6f;
        [SerializeField] private float castArcHeight = 0.35f;
        [SerializeField] private float reelDuration = 0.25f;
        [SerializeField] private float lineSag = 0.08f;
        [SerializeField] private float lineWidth = 0.03f;
        [SerializeField] private int lineSegments = 6;
        [SerializeField] private Color lineColor = new(0.94f, 0.96f, 1f, 0.85f);

        public float BoatX => boatX;
        public float BoatEnterOffsetX => boatEnterOffsetX;
        public float BoatEnterDuration => Mathf.Max(0.01f, boatEnterDuration);
        public float BoatFloatOffset => boatFloatOffset;
        public float BoatTiltScale => boatTiltScale;
        public float BoatSlopeSpan => Mathf.Max(0.01f, boatSlopeSpan);
        public float BoatFloatSmoothing => Mathf.Max(0f, boatFloatSmoothing);
        public Vector2 RodTipOffset => rodTipOffset;
        public float LineAnchorOffset => lineAnchorOffset;
        public float HookScale => Mathf.Max(0.01f, hookScale);
        public float HookRestOffset => Mathf.Max(0f, hookRestOffset);
        public float CastDuration => Mathf.Max(0.01f, castDuration);
        public float CastArcHeight => castArcHeight;
        public float ReelDuration => Mathf.Max(0.01f, reelDuration);
        public float LineSag => Mathf.Max(0f, lineSag);
        public float LineWidth => Mathf.Max(0.001f, lineWidth);
        public int LineSegments => Mathf.Clamp(lineSegments, 2, 32);
        public Color LineColor => lineColor;

        protected override string DefaultDisplayName => "낚싯줄";

        protected override IEnumerable<BossPattern> CreateDefaultPatterns()
        {
            yield return new BossPattern("Top", BossLanes.Mask(0), 3f);
            yield return new BossPattern("Middle", BossLanes.Mask(1), 3f);
            yield return new BossPattern("Bottom", BossLanes.Mask(2), 3f);
            yield return new BossPattern("TopMiddle", BossLanes.Mask(0, 1), 2f);
            yield return new BossPattern("MiddleBottom", BossLanes.Mask(1, 2), 2f);
            yield return new BossPattern("TopBottom", BossLanes.Mask(0, 2), 2f);
        }
    }
}
