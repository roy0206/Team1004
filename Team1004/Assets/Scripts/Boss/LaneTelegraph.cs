using System.Collections.Generic;
using UnityEngine;

namespace Game.Boss
{
    public sealed class LaneTelegraph : MonoThing
    {
        [SerializeField] private SpriteRenderer[] laneRenderers;
        [SerializeField] private float pulseDuration = 0.4f;
        [SerializeField] private float minAlpha = 0.3f;
        [SerializeField] private float maxAlpha = 0.4f;
        [SerializeField] private float brightAlpha = 0.45f;

        private LaneTelegraphModule module;

        public int LaneCount => laneRenderers != null ? laneRenderers.Length : 0;
        public int Mask => module != null ? module.Mask : 0;
        public bool IsShowing => module != null && module.IsShowing;
        public bool IsBright => module != null && module.IsBright;

        public SpriteRenderer GetLaneRenderer(int lane)
        {
            return laneRenderers != null && lane >= 0 && lane < laneRenderers.Length ? laneRenderers[lane] : null;
        }

        public bool TryGetLaneBand(int lane, out Rect band)
        {
            var renderer = GetLaneRenderer(lane);

            if (renderer == null)
            {
                band = default;
                return false;
            }

            var bounds = renderer.bounds;
            band = new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
            return true;
        }

        public void ApplyLayout(IReadOnlyList<float> laneY, float width, float height)
        {
            if (laneRenderers == null || laneY == null || width <= 0f || height <= 0f)
                return;

            for (var lane = 0; lane < laneRenderers.Length; lane++)
            {
                var renderer = laneRenderers[lane];

                if (renderer == null || lane >= laneY.Count)
                    continue;

                var target = renderer.transform;
                target.position = new Vector3(0f, laneY[lane], target.position.z);

                var bounds = renderer.sprite != null ? renderer.sprite.bounds.size : Vector3.one;
                var scaleX = bounds.x > 0f ? width / bounds.x : 1f;
                var scaleY = bounds.y > 0f ? height / bounds.y : 1f;
                target.localScale = new Vector3(scaleX, scaleY, 1f);
            }
        }

        public void Show(int lane)
        {
            ShowMask(BossLanes.Mask(lane));
        }

        public void ShowMask(int laneMask)
        {
            EnsureModule().Show(laneMask);
        }

        public void Brighten()
        {
            if (module != null)
                module.Brighten();
        }

        public void Hide()
        {
            if (module != null)
                module.Hide();
        }

        protected override void OnThingDestroy()
        {
            module = null;
        }

        private LaneTelegraphModule EnsureModule()
        {
            if (module == null)
                module = AddModule(new LaneTelegraphModule(laneRenderers, pulseDuration, minAlpha, maxAlpha, brightAlpha));

            return module;
        }
    }
}
