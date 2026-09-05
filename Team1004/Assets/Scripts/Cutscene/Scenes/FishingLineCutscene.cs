using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Game.Cutscene.Scenes
{
    public sealed class FishingLineCutscene : CutsceneBase
    {
        private const string Line01 = "cutscene1.01";
        private const string Line02 = "cutscene1.02";
        private const string Line03 = "cutscene1.03";
        private const string Line04 = "cutscene1.04";

        private static readonly string[] Lines = { Line01, Line02, Line03, Line04 };

        private static readonly Vector2 BossStart = new(1.5f, CutsceneStageLayout.OffscreenTopY);
        private static readonly Vector2 BossEnd = new(1.5f, 1f);
        private static readonly Vector2 MemoryChildSpot = new(0.6f, -0.2f);
        private static readonly Vector2 StepBack = new(-0.4f, 0f);
        private static readonly Vector2 StepForward = new(0.4f, 0f);

        private const float DescendDuration = 1.5f;
        private const float ShakeDuration = 0.3f;
        private const float ShakeStrength = 0.06f;
        private const float MemoryAlpha = 0.65f;
        private const float MemoryFadeDuration = 0.4f;
        private const float StepBackDuration = 0.5f;
        private const float StepForwardDuration = 0.4f;

        public override string Id => CutsceneCatalog.FishingLine;

        public override IReadOnlyList<string> LineIds => Lines;

        protected override async Awaitable Run()
        {
            await Move(Boss, BossStart);
            await Show(Boss);

            await Together(
                Move(Boss, BossEnd, DescendDuration, Ease.OutQuad),
                Shake(ShakeDuration, ShakeStrength));

            await Say(Line01);

            await Move(Player, StepBack, StepBackDuration, Ease.OutQuad, true);

            await Move(Child, MemoryChildSpot);
            await FadeActor(Child, 0f);
            await Show(Child);

            await Together(
                FadeScreen(MemoryAlpha, MemoryFadeDuration),
                FadeActor(Child, 1f, MemoryFadeDuration));

            await Say(Line02);

            await Together(
                FadeScreen(0f, MemoryFadeDuration),
                FadeActor(Child, 0f, MemoryFadeDuration));

            await Hide(Child);
            await FadeActor(Child, 1f);

            await Say(Line03);

            await Together(
                Move(Player, StepForward, StepForwardDuration, Ease.OutQuad, true),
                Say(Line04));
        }
    }
}
