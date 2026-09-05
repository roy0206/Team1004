using DG.Tweening;
using UnityEngine;

namespace Game.Boss
{
    public sealed class LaneTelegraphModule : Module
    {
        private readonly SpriteRenderer[] renderers;
        private readonly Tween[] tweens;
        private readonly float pulseDuration;
        private readonly float minAlpha;
        private readonly float maxAlpha;
        private readonly float brightAlpha;
        private int mask;
        private bool bright;

        public LaneTelegraphModule(SpriteRenderer[] renderers, float pulseDuration, float minAlpha, float maxAlpha, float brightAlpha)
        {
            this.renderers = renderers ?? new SpriteRenderer[0];
            tweens = new Tween[this.renderers.Length];
            this.pulseDuration = pulseDuration;
            this.minAlpha = minAlpha;
            this.maxAlpha = maxAlpha;
            this.brightAlpha = brightAlpha;
        }

        public int Mask => mask;
        public bool IsShowing => mask != 0;
        public bool IsBright => bright;
        public int LaneCount => renderers.Length;

        protected override ModuleTick Ticks => ModuleTick.None;

        public void Show(int laneMask)
        {
            mask = laneMask;
            bright = false;

            for (var lane = 0; lane < renderers.Length; lane++)
            {
                var renderer = renderers[lane];

                if (renderer == null)
                    continue;

                var on = BossLanes.Contains(laneMask, lane);
                KillTween(lane);

                if (!on)
                {
                    renderer.enabled = false;
                    continue;
                }

                renderer.enabled = true;
                SetAlpha(renderer, minAlpha);

                if (pulseDuration > 0f)
                    tweens[lane] = DOTween.ToAlpha(() => renderer.color, value => renderer.color = value, maxAlpha, pulseDuration)
                        .SetLoops(-1, LoopType.Yoyo);
                else
                    SetAlpha(renderer, maxAlpha);
            }
        }

        public void Brighten()
        {
            if (mask == 0 || bright)
                return;

            bright = true;

            for (var lane = 0; lane < renderers.Length; lane++)
            {
                var renderer = renderers[lane];

                if (renderer == null || !BossLanes.Contains(mask, lane))
                    continue;

                KillTween(lane);
                SetAlpha(renderer, brightAlpha);
            }
        }

        public void Hide()
        {
            mask = 0;
            bright = false;

            for (var lane = 0; lane < renderers.Length; lane++)
            {
                KillTween(lane);

                if (renderers[lane] != null)
                    renderers[lane].enabled = false;
            }
        }

        protected override void OnDetached()
        {
            Hide();
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            var color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }

        private void KillTween(int lane)
        {
            var tween = tweens[lane];

            if (tween != null && tween.IsActive())
                tween.Kill();

            tweens[lane] = null;
        }
    }
}
