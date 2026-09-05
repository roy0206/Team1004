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
        private const string Line07 = "intro.07";

        private static readonly string[] Lines =
        {
            Line01, Line02, Line03, Line04, Line05, Line06, Line07
        };

        private static readonly Vector2 PoolSpot = new(0.6f, -1.5f);
        private static readonly Vector2 TrappedSpot = new(0.6f, -1.3f);
        private static readonly Vector2 ChildFar = new(3.4f, -0.4f);
        private static readonly Vector2 ChildNear = new(1.9f, -0.6f);
        private static readonly Vector2 ReleaseSpot = new(-1.4f, 0.2f);
        private static readonly Vector2 OffscreenLeft = new(CutsceneStageLayout.OffscreenLeftX, CutsceneStageLayout.LaneY);
        private static readonly Vector2 PlayerHome = new(CutsceneStageLayout.PlayerHomeX, CutsceneStageLayout.LaneY);
        private static readonly Vector2 CameraOffset = new(1.5f, 0.3f);

        private const float BlackoutIn = 0.4f;
        private const float BlackoutOut = 0.5f;
        private const float BlackoutHold = 0.3f;
        private const float ChildApproachDuration = 0.8f;
        private const float ReleaseDuration = 1f;
        private const float ChildLeaveDuration = 0.8f;
        private const float WalkInDuration = 1.2f;
        private const float CameraPanDuration = 1.2f;

        public override string Id => CutsceneCatalog.Intro;

        public override IReadOnlyList<string> LineIds => Lines;

        protected override async Awaitable Run()
        {
            await FadeScreen(1f);
            await Show(Landmark);
            await Move(Landmark, PoolSpot);
            await Show(Child);
            await Move(Child, ChildFar);
            await Move(Player, TrappedSpot);
            await SetPose(Player, PlayerSwimClip, 0);
            await FadeScreen(0f, BlackoutOut);

            await Say(Line01);

            await Move(Child, ChildNear, ChildApproachDuration, Ease.OutQuad);

            await Say(Line02);
            await Say(Line03);
            await Say(Line04);

            await Together(
                Move(Player, ReleaseSpot, ReleaseDuration, Ease.OutQuad),
                Animate(Player, PlayerJumpClip, ReleaseDuration));

            await SetPose(Player, PlayerSwimClip, 0);

            await Say(Line05);

            await Move(Child, ChildFar, ChildLeaveDuration, Ease.InOutSine);

            await FadeScreen(1f, BlackoutIn);
            await Hide(Child);
            await Hide(Landmark);
            await Move(Player, OffscreenLeft);
            await Wait(BlackoutHold);

            await Say(Line06);

            await Together(
                FadeScreen(0f, BlackoutOut),
                Move(Player, PlayerHome, WalkInDuration, Ease.OutQuad));

            await Together(
                CameraTo(CameraOffset, 0f, CameraPanDuration, Ease.InOutSine, true),
                Say(Line07));
        }
    }
}
