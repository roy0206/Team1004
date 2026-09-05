using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Game.Cutscene.Scenes
{
    public sealed class EndingCutscene : CutsceneBase
    {
        private const string Line01 = "ending.01";
        private const string Line02 = "ending.02";
        private const string Line03 = "ending.03";
        private const string Line04 = "ending.04";

        private static readonly string[] Lines = { Line01, Line02, Line03, Line04 };

        private static readonly Vector2 LandmarkSpot = new(2f, -0.8f);
        private static readonly Vector2 ArrivalSpot = new(0.5f, -0.3f);
        private static readonly Vector2 CameraOffset = new(1f, 0f);

        private const float ArrivalDuration = 2.5f;
        private const float ArrivalOrthoSize = 3f;
        private const float MemoryFadeDuration = 0.6f;
        private const float MemoryAlpha = 0.5f;
        private const float BeforeBlackout = 0.6f;
        private const float BlackoutDuration = 1f;

        public override string Id => CutsceneCatalog.Ending;

        public override IReadOnlyList<string> LineIds => Lines;

        protected override async Awaitable Run()
        {
            await Show(Landmark);
            await Move(Landmark, LandmarkSpot);

            await Together(
                Move(Player, ArrivalSpot, ArrivalDuration),
                CameraTo(CameraOffset, ArrivalOrthoSize, ArrivalDuration, Ease.InOutSine, true));

            await Animate(Player, PlayerJumpClip);

            await FadeScreen(MemoryAlpha, MemoryFadeDuration);
            await Say(Line01);
            await FadeScreen(0f, MemoryFadeDuration);

            await Say(Line02);
            await Say(Line03);

            await Wait(BeforeBlackout);
            await FadeScreen(1f, BlackoutDuration);
            await Say(Line04);
        }
    }
}
