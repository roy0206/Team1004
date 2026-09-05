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
        private const string Line04 = "cutscene3.04";

        private static readonly string[] Lines = { Line01, Line02, Line03, Line04 };

        private static readonly Vector2 BossStart = new(CutsceneStageLayout.OffscreenRightX + 0.5f, 0.5f);
        private static readonly Vector2 BossEnd = new(4f, 0.5f);
        private static readonly Vector2 Drift = new(-0.7f, -0.3f);
        private static readonly Vector2 Recover = new(0.5f, 0.3f);
        private static readonly Vector2 LookUpOffset = new(0f, 0.8f);

        private const float DriftDuration = 1.4f;
        private const float ApproachDuration = 1.5f;
        private const float ShakeDuration = 1f;
        private const float ShakeStrength = 0.12f;
        private const float LookUpDuration = 1f;
        private const float DoubtHold = 0.8f;
        private const float RecoverDuration = 0.6f;

        public override string Id => CutsceneCatalog.Waterfall;

        public override IReadOnlyList<string> LineIds => Lines;

        protected override async Awaitable Run()
        {
            await SetPose(Player, PlayerSwimClip, 0);

            await Together(
                Move(Player, Drift, DriftDuration, Ease.OutSine, true),
                Say(Line01));

            await Move(Boss, BossStart);
            await Show(Boss);

            await Together(
                Move(Boss, BossEnd, ApproachDuration, Ease.OutQuad),
                CameraTo(LookUpOffset, 0f, LookUpDuration, Ease.InOutSine, true),
                Shake(ShakeDuration, ShakeStrength));

            await Say(Line02);

            await Wait(DoubtHold);

            await Say(Line03);

            await Together(
                Move(Player, Recover, RecoverDuration, Ease.OutQuad, true),
                Say(Line04));
        }
    }
}
