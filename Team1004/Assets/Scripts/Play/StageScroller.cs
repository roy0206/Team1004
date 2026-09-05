using Game.Config;
using UnityEngine;

namespace Game.Play
{
    public sealed class StageScroller : MonoThing
    {
        private ScrollModule scroll;

        public ScrollModule Scroll => scroll;
        public float Scrolled => scroll != null ? scroll.Scrolled : 0f;
        public bool IsScrolling => scroll != null && scroll.IsScrolling;

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

        private void OnConfigChanged()
        {
            if (scroll != null)
                scroll.Speed = GameConfig.Current.ScrollSpeed;
        }
    }
}
