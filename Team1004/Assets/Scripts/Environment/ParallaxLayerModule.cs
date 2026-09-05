using UnityEngine;

namespace Game.Environment
{
    public sealed class ParallaxLayerModule : Module
    {
        private readonly TileStripRunner strip;
        private readonly float speedScale;

        public float Speed { get; set; }
        public bool IsScrolling { get; set; }
        public float SpeedScale => speedScale;
        public TileStripRunner Strip => strip;

        public ParallaxLayerModule(TileStripRunner strip, float speedScale, float speed)
        {
            this.strip = strip;
            this.speedScale = speedScale;
            Speed = speed;
        }

        protected override ModuleTick Ticks => ModuleTick.Update;

        protected override void OnUpdate()
        {
            if (!IsScrolling || strip == null)
                return;

            var distance = Speed * speedScale * Time.deltaTime;

            if (distance <= 0f)
                return;

            strip.Advance(distance);
        }

        public void Rewind()
        {
            strip?.Rewind();
        }
    }
}
