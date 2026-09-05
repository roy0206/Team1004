using System;
using UnityEngine;

namespace Game.Water
{
    public sealed class WaterInteractorModule : Module, IWaterCollider
    {
        private readonly Transform target;
        private readonly Collider2D shape;
        private readonly Vector2 localSize;
        private readonly Vector2 localOffset;
        private readonly VelocitySampler sampler;

        private float surfaceInfluence;
        private bool wakeOnly;
        private float wakeScale = 1f;
        private int warmupRemaining;

        public WaterInteractorModule(
            Transform target, Vector2 localSize, Vector2 localOffset, float smoothing,
            float surfaceInfluence = 0f, bool wakeOnly = false, float wakeScale = 1f)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.localSize = new Vector2(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y));
            this.localOffset = localOffset;
            this.surfaceInfluence = surfaceInfluence;
            this.wakeOnly = wakeOnly;
            this.wakeScale = Mathf.Max(0f, wakeScale);
            sampler = new VelocitySampler(smoothing);
        }

        public WaterInteractorModule(
            Transform target, Collider2D shape, float smoothing,
            float surfaceInfluence = 0f, bool wakeOnly = false, float wakeScale = 1f)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.surfaceInfluence = surfaceInfluence;
            this.wakeOnly = wakeOnly;
            this.wakeScale = Mathf.Max(0f, wakeScale);
            sampler = new VelocitySampler(smoothing);

            if (shape is BoxCollider2D box)
            {
                localSize = new Vector2(Mathf.Abs(box.size.x), Mathf.Abs(box.size.y));
                localOffset = box.offset;
                return;
            }

            this.shape = shape;
            localSize = Vector2.one;
        }

        public float VerticalVelocity => sampler.Velocity.y;
        public float HorizontalVelocity => sampler.Velocity.x;
        public Vector2 Velocity => sampler.Velocity;
        public Vector2 LocalSize => localSize;
        public Vector2 LocalOffset => localOffset;
        public bool UsesColliderShape => shape != null;
        public bool IsTouchingWater { get; private set; }
        public bool TransfersVerticalVelocity => !wakeOnly;
        public int WarmupRemaining => warmupRemaining;
        public bool IsWarmingUp => warmupRemaining > 0;

        public bool WakeOnly
        {
            get => wakeOnly;
            set => wakeOnly = value;
        }

        public float WakeScale
        {
            get => wakeScale;
            set => wakeScale = Mathf.Max(0f, value);
        }

        public float SurfaceInfluence
        {
            get
            {
                if (surfaceInfluence > 0f)
                    return surfaceInfluence;

                var settings = WaterSettings.currentSettings;
                return settings != null ? settings.wakeInfluenceDistance : 0f;
            }
            set => surfaceInfluence = value;
        }

        public Bounds Bounds
        {
            get
            {
                if (shape != null)
                    return shape.bounds;

                var scale = target.lossyScale;
                var center = target.TransformPoint(localOffset);
                var size = new Vector3(localSize.x * Mathf.Abs(scale.x), localSize.y * Mathf.Abs(scale.y), 0f);
                return new Bounds(center, size);
            }
        }

        protected override ModuleTick Ticks => ModuleTick.Update;

        public bool OverlapPoint(Vector2 point)
        {
            if (shape != null)
                return shape.OverlapPoint(point);

            var bounds = Bounds;
            return point.x >= bounds.min.x && point.x <= bounds.max.x &&
                   point.y >= bounds.min.y && point.y <= bounds.max.y;
        }

        public void ResetVelocity()
        {
            sampler.Reset(target != null ? (Vector2)target.position : Vector2.zero);
            BeginWarmup();
        }

        public void NotifyTeleport()
        {
            ResetVelocity();
        }

        public void BeginWarmup()
        {
            var settings = WaterSettings.currentSettings;
            warmupRemaining = settings != null ? Mathf.Max(0, settings.InjectionWarmupFrames) : 0;
            IsTouchingWater = false;
        }

        public bool Step(float deltaTime)
        {
            if (target == null)
                return false;

            var settings = WaterSettings.currentSettings;
            var teleport = settings != null ? settings.TeleportDistance : 0f;
            var position = (Vector2)target.position;

            if (teleport > 0f && sampler.HasSample &&
                (position - sampler.Position).sqrMagnitude > teleport * teleport)
            {
                sampler.Reset(position);
                BeginWarmup();
                return false;
            }

            sampler.Sample(position, deltaTime);

            if (warmupRemaining > 0)
            {
                warmupRemaining--;
                IsTouchingWater = false;
                return false;
            }

            IsTouchingWater = WaterSystem.CollideAll(this);
            return true;
        }

        protected override void OnAttached()
        {
            ResetVelocity();
            IsTouchingWater = false;
        }

        protected override void OnDetached()
        {
            IsTouchingWater = false;
        }

        protected override void OnUpdate()
        {
            Step(Time.deltaTime);
        }
    }
}
