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
        private readonly VerticalVelocitySampler sampler;

        public WaterInteractorModule(Transform target, Vector2 localSize, Vector2 localOffset, float smoothing)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.localSize = new Vector2(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y));
            this.localOffset = localOffset;
            sampler = new VerticalVelocitySampler(smoothing);
        }

        public WaterInteractorModule(Transform target, Collider2D shape, float smoothing)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            sampler = new VerticalVelocitySampler(smoothing);

            if (shape is BoxCollider2D box)
            {
                localSize = new Vector2(Mathf.Abs(box.size.x), Mathf.Abs(box.size.y));
                localOffset = box.offset;
                return;
            }

            this.shape = shape;
            localSize = Vector2.one;
        }

        public float VerticalVelocity => sampler.Velocity;
        public Vector2 LocalSize => localSize;
        public Vector2 LocalOffset => localOffset;
        public bool UsesColliderShape => shape != null;
        public bool IsTouchingWater { get; private set; }

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
            sampler.Reset(target != null ? target.position.y : 0f);
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
            if (target == null)
                return;

            sampler.Sample(target.position.y, Time.deltaTime);
            IsTouchingWater = WaterSystem.CollideAll(this);
        }
    }
}
