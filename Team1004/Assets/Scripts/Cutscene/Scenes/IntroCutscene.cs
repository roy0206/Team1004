using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Game.Cutscene.Scenes
{
    public sealed class IntroCutscene : CutsceneBase
    {
        private const string Line01 = "intro.01";
        private const string Line02 = "intro.02";
        private const string Line03 = "intro.03";
        private const string Line04 = "intro.04";
        private const string Line05 = "intro.05";
        private const string Line06 = "intro.06";

        private static readonly string[] Lines = { Line01, Line02, Line03, Line04, Line05, Line06 };

        private static readonly Vector2 ChildLandmark = new(1.5f, -1.2f);
        private static readonly Vector2 ChildPlayer = new(-1.2f, -0.6f);
        private static readonly Vector2 OffscreenLeft = new(CutsceneStageLayout.OffscreenLeftX, CutsceneStageLayout.LaneY);
        private static readonly Vector2 PlayerHome = new(CutsceneStageLayout.PlayerHomeX, CutsceneStageLayout.LaneY);
        private static readonly Vector2 CameraOffset = new(1.5f, 0.3f);

        private const float BlackoutIn = 0.5f;
        private const float BlackoutOut = 0.6f;
        private const float BlackoutHold = 0.3f;
        private const float WalkInDuration = 1.5f;
        private const float CameraPanDuration = 1.2f;
        private const float ShakeDuration = 0.3f;
        private const float ShakeStrength = 0.08f;
        private const float ShakeTail = 0.4f;

        public override string Id => CutsceneCatalog.Intro;

        public override IReadOnlyList<string> LineIds => Lines;

        protected override async Awaitable Run()
        {
            await FadeScreen(1f);
            await Show(Landmark);
            await Move(Landmark, ChildLandmark);
            await Move(Player, ChildPlayer);
            await FadeScreen(0f, BlackoutOut);

            await Say(Line01);
            await Say(Line02);

            await FadeScreen(1f, BlackoutIn);
            await Hide(Landmark);
            await Move(Player, OffscreenLeft);
            await Wait(BlackoutHold);

            await Together(
                FadeScreen(0f, BlackoutOut),
                Move(Player, PlayerHome, WalkInDuration, Ease.OutQuad));

            await Say(Line03);

            await Together(
                CameraTo(CameraOffset, 0f, CameraPanDuration, Ease.InOutSine, true),
                Say(Line04));

            await Say(Line05);

            await Together(
                Shake(ShakeDuration, ShakeStrength),
                Wait(ShakeTail));

            await Say(Line06);
        }
    }
}
