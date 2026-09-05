using Game.Config;
using Game.Particles;
using UnityEngine;

namespace Game.Environment
{
    public sealed class UnderwaterFlowModule : Module
    {
        private readonly EnvironmentThing environment;
        private readonly SpriteEmitterModule[] emitters;
        private readonly float holdSpeedScale;

        private float ambientSpeedScale = 1f;

        public UnderwaterFlowModule(EnvironmentThing environment, SpriteEmitterModule[] emitters, float holdSpeedScale)
        {
            this.environment = environment;
            this.emitters = emitters ?? System.Array.Empty<SpriteEmitterModule>();
            this.holdSpeedScale = Mathf.Clamp01(holdSpeedScale);
        }

        public float AmbientSpeedScale
        {
            get => ambientSpeedScale;
            set => ambientSpeedScale = Mathf.Max(0f, value);
        }

        public float HoldSpeedScale => holdSpeedScale;
        public float FlowSpeed { get; private set; }
        public float SurfaceY { get; private set; }

        protected override ModuleTick Ticks => ModuleTick.Update;

        protected override void OnAttached()
        {
            Apply();
        }

        protected override void OnUpdate()
        {
            Apply();
        }

        public void Apply()
        {
            var scrollScale = environment != null ? Mathf.Max(0f, environment.ScrollScale) : 1f;
            var scroll = environment != null ? Mathf.Max(0f, environment.Speed) * scrollScale : 0f;
            var floor = GameConfig.Current.ScrollSpeed * scrollScale * holdSpeedScale;

            FlowSpeed = Mathf.Max(scroll, floor);
            SurfaceY = environment != null ? environment.SurfaceY : GameConfig.Current.WaterSurfaceY;

            var flow = new Vector2(-FlowSpeed, 0f);

            for (var i = 0; i < emitters.Length; i++)
            {
                var emitter = emitters[i];

                if (emitter == null)
                    continue;

                emitter.ExternalVelocity = flow;
                emitter.SpeedScale = ambientSpeedScale;
                emitter.SurfaceY = SurfaceY;
            }
        }
    }
}
