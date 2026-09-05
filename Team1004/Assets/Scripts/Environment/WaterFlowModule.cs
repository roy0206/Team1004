using UnityEngine;

namespace Game.Environment
{
    public sealed class WaterFlowModule : Module
    {
        private readonly TileStripRunner strip;
        private readonly float speedScale;
        private readonly float baseFlowSpeed;

        public float Speed { get; set; }
        public bool IsScrolling { get; set; }
        public float SpeedScale => speedScale;
        public float BaseFlowSpeed => baseFlowSpeed;
        public float TimeScale { get; set; } = 1f;
        public TileStripRunner Strip => strip;

        public WaterFlowModule(TileStripRunner strip, float speedScale, float baseFlowSpeed, float speed)
        {
            this.strip = strip;
            this.speedScale = speedScale;
            this.baseFlowSpeed = Mathf.Max(0f, baseFlowSpeed);
            Speed = speed;
        }

        protected override ModuleTick Ticks => ModuleTick.Update;

        protected override void OnUpdate()
        {
            if (strip == null)
                return;

            var flow = baseFlowSpeed;

            if (IsScrolling)
                flow += Speed * speedScale;

            var distance = flow * TimeScale * Time.deltaTime;

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
