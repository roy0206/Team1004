using UnityEngine;

namespace Game.Environment
{
    public sealed class HomelandModule : Module
    {
        private readonly GameObject root;
        private readonly TileStripRunner strip;
        private readonly float speedScale;

        public float Speed { get; set; }
        public bool IsScrolling { get; set; }
        public float SpeedScale => speedScale;
        public bool IsVisible { get; private set; }
        public TileStripRunner Strip => strip;

        public HomelandModule(GameObject root, TileStripRunner strip, float speedScale, float speed)
        {
            this.root = root;
            this.strip = strip;
            this.speedScale = speedScale;
            Speed = speed;
            IsVisible = root != null && root.activeSelf;
        }

        protected override ModuleTick Ticks => ModuleTick.Update;

        public void SetVisible(bool visible)
        {
            if (IsVisible == visible)
                return;

            IsVisible = visible;

            if (visible)
                strip?.Rewind();

            if (root != null)
                root.SetActive(visible);
        }

        protected override void OnUpdate()
        {
            if (!IsVisible || !IsScrolling || strip == null)
                return;

            var distance = Speed * speedScale * Time.deltaTime;

            if (distance <= 0f)
                return;

            strip.Advance(distance);
        }
    }
}
