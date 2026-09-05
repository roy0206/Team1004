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
        private const string Line05 = "ending.05";

        private static readonly string[] Lines = { Line01, Line02, Line03, Line04, Line05 };

        private static readonly Vector2 LandmarkSpot = new(2f, -0.8f);
        private static readonly Vector2 ArrivalSpot = new(0.5f, -0.3f);
        private static readonly Vector2 CameraOffset = new(1f, 0f);
        private static readonly Vector2 MemoryChildSpot = new(0.5f, 0.4f);
        private static readonly Vector2 GrownChildStart = new(CutsceneStageLayout.OffscreenRightX, -0.5f);
        private static readonly Vector2 GrownChildSpot = new(3f, -0.5f);

        private const float ArrivalDuration = 2.5f;
        private const float ArrivalOrthoSize = 3f;
        private const float MemoryFadeDuration = 0.6f;
        private const float MemoryAlpha = 0.6f;
        private const float ChildWalkDuration = 1.6f;
        private const float BeforeBlackout = 0.8f;
        private const float BlackoutDuration = 1.2f;

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
            await SetPose(Player, PlayerSwimClip, 0);

            await Say(Line01);

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

            await Move(Child, GrownChildStart);

            await Together(
                Move(Child, GrownChildSpot, ChildWalkDuration, Ease.OutQuad),
                FadeActor(Child, 1f, MemoryFadeDuration));

            await Say(Line03);
            await Say(Line04);

            await Wait(BeforeBlackout);
            await FadeScreen(1f, BlackoutDuration);
            await Say(Line05);
        }
    }
}
