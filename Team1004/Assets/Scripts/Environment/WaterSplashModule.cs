using UnityEngine;
using WaterSurface = Game.Water.Water;
using WaterSystem = Game.Water.WaterSystem;

namespace Game.Environment
{
    public sealed class WaterSplashModule : Module
    {
        private readonly WaterSurface surface;
        private readonly float radius;
        private readonly float maxDepth;

        public WaterSurface Surface => surface;
        public float Radius => radius;
        public float MaxDepth => maxDepth;

        public WaterSplashModule(WaterSurface surface, float radius, float maxDepth)
        {
            this.surface = surface;
            this.radius = Mathf.Max(0.01f, radius);
            this.maxDepth = Mathf.Max(0f, maxDepth);
        }

        protected override ModuleTick Ticks => ModuleTick.None;

        public bool Splash(float x, float strength)
        {
            if (surface == null || maxDepth <= 0f)
                return false;

            if (!WaterSystem.GetPositions(surface, out var positions, out var range) || range.length <= 0)
                return false;

            var target = surface.OuterIndexRange(x - radius, x + radius);
            var start = Mathf.Max(range.start, target.start);
            var end = Mathf.Min(range.end, target.end);

            if (start >= end)
                return false;

            var depth = Mathf.Clamp(strength, -maxDepth, maxDepth);

            for (var i = start; i < end; i++)
            {
                var falloff = 1f - Mathf.Abs(surface.IndexToX(i) - x) / radius;

                if (falloff <= 0f)
                    continue;

                var index = i - range.start;

                if (index < 0 || index >= positions.Length)
                    continue;

                positions[index] -= depth * falloff * falloff;
            }

            return true;
        }
    }
}
