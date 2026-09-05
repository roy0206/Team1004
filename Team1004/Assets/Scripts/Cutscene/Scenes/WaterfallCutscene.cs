using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Game.Cutscene.Scenes
{
    public sealed class WaterfallCutscene : CutsceneBase
    {
        private const string Line01 = "cutscene3.01";
        private const string Line02 = "cutscene3.02";
        private const string Line03 = "cutscene3.03";

        private static readonly string[] Lines = { Line01, Line02, Line03 };

        private static readonly Vector2 LandmarkSpot = new(0.5f, -1.4f);
        private static readonly Vector2 BossStart = new(CutsceneStageLayout.OffscreenRightX + 0.5f, 0.5f);
        private static readonly Vector2 BossEnd = new(4f, 0.5f);

        private const float ApproachDuration = 1.5f;
        private const float ShakeDuration = 1f;
        private const float ShakeStrength = 0.12f;

        public override string Id => CutsceneCatalog.Waterfall;

        public override IReadOnlyList<string> LineIds => Lines;

        protected override async Awaitable Run()
        {
            await Show(Landmark);
            await Move(Landmark, LandmarkSpot);

            await Say(Line01);

            await Move(Boss, BossStart);
            await Show(Boss);

            await Together(
                Move(Boss, BossEnd, ApproachDuration, Ease.OutQuad),
                Shake(ShakeDuration, ShakeStrength));

            await Say(Line02);
            await Say(Line03);
        }
    }
}
