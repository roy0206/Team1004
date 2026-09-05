using Game.Water;
using UnityEngine;
using WaterVolume = Game.Water.Water;

namespace Game.Boss
{
    public sealed class BoatFloatModule : Module
    {
        private readonly Transform boat;

        private WaterVolume water;
        private float height;
        private float tilt;
        private bool settled;

        public BoatFloatModule(Transform boat)
        {
            this.boat = boat;
        }

        public float FloatOffset { get; set; }
        public float TiltScale { get; set; } = 1f;
        public float SlopeSpan { get; set; } = 0.8f;
        public float Smoothing { get; set; } = 12f;
        public float FallbackHeight { get; set; }
        public float Height => height;
        public float Tilt => tilt;

        protected override ModuleTick Ticks => ModuleTick.LateUpdate;

        public void Snap()
        {
            settled = false;
        }

        protected override void OnLateUpdate()
        {
            if (boat == null)
                return;

            if (water == null)
                water = WaterSurfaceSampler.Find();

            var x = boat.position.x;
            var span = Mathf.Max(0.01f, SlopeSpan);
            var targetHeight = Sample(x) + FloatOffset;
            var targetTilt = Mathf.Atan2(Sample(x + span) - Sample(x - span), span * 2f) * Mathf.Rad2Deg * TiltScale;

            if (settled)
            {
                var blend = Smoothing <= 0f ? 1f : 1f - Mathf.Exp(-Smoothing * Time.deltaTime);
                height = Mathf.Lerp(height, targetHeight, blend);
                tilt = Mathf.Lerp(tilt, targetTilt, blend);
            }
            else
            {
                settled = true;
                height = targetHeight;
                tilt = targetTilt;
            }

            var position = boat.position;
            boat.position = new Vector3(position.x, height, position.z);
            boat.rotation = Quaternion.Euler(0f, 0f, tilt);
        }

        private float Sample(float x)
        {
            return WaterSurfaceSampler.TrySampleHeight(water, x, out var value) ? value : FallbackHeight;
        }
    }
}
