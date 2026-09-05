using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Game.Cutscene.Scenes
{
    public sealed class WalrusCutscene : CutsceneBase
    {
        private const string Line01 = "cutscene2.01";
        private const string Line02 = "cutscene2.02";
        private const string Line03 = "cutscene2.03";
        private const string Line04 = "cutscene2.04";

        private static readonly string[] Lines = { Line01, Line02, Line03, Line04 };

        private static readonly Vector2 BossStart = new(CutsceneStageLayout.OffscreenRightX, 0f);
        private static readonly Vector2 BossEnd = new(3.2f, 0f);
        private static readonly Vector2 StepBack = new(-0.6f, 0f);
        private static readonly Vector2 StepForward = new(0.6f, 0f);

        private const float ApproachDuration = 2f;
        private const float ShakeDuration = 0.6f;
        private const float ShakeStrength = 0.12f;
        private const float StepBackDuration = 0.6f;
        private const float StepForwardDuration = 0.5f;

        public override string Id => CutsceneCatalog.Walrus;

        public override IReadOnlyList<string> LineIds => Lines;

        protected override async Awaitable Run()
        {
            await Move(Boss, BossStart);
            await Show(Boss);

            await Together(
                Move(Boss, BossEnd, ApproachDuration, Ease.OutQuad),
                Shake(ShakeDuration, ShakeStrength));

            await Say(Line01);
            await Move(Player, StepBack, StepBackDuration, Ease.OutQuad, true);
            await Say(Line02);

            await Together(
                Move(Player, StepForward, StepForwardDuration, Ease.OutQuad, true),
                Say(Line03));

            await Say(Line04);
        }
    }
}
