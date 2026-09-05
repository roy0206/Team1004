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
