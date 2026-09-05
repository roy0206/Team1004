using Game.Config;
using UnityEngine;

namespace Game.Play
{
    public sealed class StageScroller : MonoThing
    {
        private ScrollModule scroll;
        private float speedScale = 1f;

        public ScrollModule Scroll => scroll;
        public float Scrolled => scroll != null ? scroll.Scrolled : 0f;
        public bool IsScrolling => scroll != null && scroll.IsScrolling;
        public float SpeedScale => speedScale;

        private void Awake()
        {
            scroll = AddModule(new ScrollModule(transform, GameConfig.Current.ScrollSpeed));
            GameConfig.Changed += OnConfigChanged;
        }

        protected override void OnThingDestroy()
        {
            GameConfig.Changed -= OnConfigChanged;
        }

        public void SetScrolling(bool scrolling)
        {
            if (scroll != null)
                scroll.IsScrolling = scrolling;
        }

        public void SetSpeedScale(float scale)
        {
            speedScale = Mathf.Max(0f, scale);
            ApplySpeed();
        }

        private void OnConfigChanged()
        {
            ApplySpeed();
        }

        private void ApplySpeed()
        {
            if (scroll != null)
                scroll.Speed = GameConfig.Current.ScrollSpeed * speedScale;
        }
    }
}
