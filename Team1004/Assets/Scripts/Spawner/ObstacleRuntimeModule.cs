using System;
using UnityEngine;

namespace Game.Spawner
{
    public sealed class ObstacleRuntimeModule : Module
    {
        public ObstacleRuntimeModule(PooledInstance instance, ObstacleTiming timing)
        {
            Instance = instance;
            Timing = timing ?? throw new ArgumentNullException(nameof(timing));
        }

        public PooledInstance Instance { get; }
        public ObstacleTiming Timing { get; }
        public float Elapsed { get; private set; }

        protected override ModuleTick Ticks => ModuleTick.None;

        public void Advance(float deltaTime, float scrollSpeed)
        {
            if (!IsAttached || deltaTime <= 0f)
                return;

            Elapsed += deltaTime;

            var extra = Timing.Speed - scrollSpeed;
            if (Mathf.Abs(extra) < 0.0001f)
                return;

            var transform = Host.transform;
            transform.position += new Vector3(-extra * deltaTime, 0f, 0f);
        }

        public bool IsOffScreen(float screenLeftX, float margin)
        {
            return IsAttached && Host.transform.position.x + Timing.Spec.HalfBody < screenLeftX - margin;
        }
    }
}
