using System;
using UnityEngine;

namespace Game.Particles
{
    public sealed class SpriteEmitterModule : Module
    {
        private const float TeleportDistance = 1.5f;
        private const float MinScale = 0.0001f;

        private struct Particle
        {
            public bool Alive;
            public float Age;
            public float Life;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Size;
            public float Phase;
        }

        private readonly Transform emitRoot;
        private readonly ParticleProfile profile;
        private readonly SpriteRenderer[] renderers;
        private readonly Transform[] transforms;
        private readonly Particle[] particles;
        private readonly float[] nativeWidths;

        private ParticleRandom random;
        private float accumulator;
        private float time;
        private float rootScale = 1f;
        private Vector2 lastRootPosition;
        private bool hasRootSample;
        private int aliveCount;

        public SpriteEmitterModule(Transform emitRoot, ParticleProfile profile, SpriteRenderer[] renderers, uint seed)
        {
            this.emitRoot = emitRoot != null ? emitRoot : throw new ArgumentNullException(nameof(emitRoot));
            this.profile = profile;
            this.renderers = renderers ?? Array.Empty<SpriteRenderer>();

            var count = this.renderers.Length;
            transforms = new Transform[count];
            particles = new Particle[count];
            nativeWidths = new float[count];
            random = new ParticleRandom(seed);
        }

        public bool IsEmitting { get; set; } = true;
        public float SpeedScale { get; set; } = 1f;
        public Vector2 ExternalVelocity { get; set; }
        public float SurfaceY { get; set; } = float.PositiveInfinity;
        public ParticleProfile Profile => profile;
        public int Capacity => particles.Length;
        public int AliveCount => aliveCount;
        public bool IsUsable => profile != null && particles.Length > 0;

        protected override ModuleTick Ticks => ModuleTick.Update;

        protected override void OnAttached()
        {
            rootScale = Mathf.Abs(emitRoot.lossyScale.x);

            if (rootScale < MinScale)
                rootScale = 1f;

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];

                if (renderer == null)
                    continue;

                transforms[i] = renderer.transform;

                if (profile != null)
                {
                    if (profile.Sprite != null)
                        renderer.sprite = profile.Sprite;

                    renderer.sortingOrder = profile.SortingOrder;
                }

                var sprite = renderer.sprite;
                nativeWidths[i] = sprite != null ? Mathf.Max(MinScale, sprite.bounds.size.x) : 1f;
                renderer.enabled = false;
            }

            Clear();
            ResetMovementSample();
        }

        protected override void OnDetached()
        {
            Clear();
        }

        protected override void OnUpdate()
        {
            Step(Time.deltaTime);
        }

        public void ResetMovementSample()
        {
            hasRootSample = false;
            accumulator = 0f;
        }

        public void Clear()
        {
            for (var i = 0; i < particles.Length; i++)
            {
                particles[i].Alive = false;

                if (renderers[i] != null)
                    renderers[i].enabled = false;
            }

            aliveCount = 0;
            accumulator = 0f;
        }

        public void Burst(int count)
        {
            if (!IsUsable || count <= 0)
                return;

            for (var i = 0; i < count; i++)
            {
                if (!TrySpawn())
                    return;
            }
        }

        public void Step(float deltaTime)
        {
            if (!IsUsable)
                return;

            var scaled = deltaTime * Mathf.Max(0f, SpeedScale);
            var moved = SampleMovement();

            if (scaled > 0f)
            {
                time += scaled;
                Advance(scaled);
            }

            if (IsEmitting)
                Emit(scaled, moved);

            Render();
        }

        private float SampleMovement()
        {
            var position = (Vector2)emitRoot.position;

            if (!hasRootSample)
            {
                hasRootSample = true;
                lastRootPosition = position;
                return 0f;
            }

            var delta = position - lastRootPosition;
            lastRootPosition = position;

            var distance = delta.magnitude;
            return distance > TeleportDistance ? 0f : distance;
        }

        private void Emit(float scaledDeltaTime, float movedDistance)
        {
            var mode = profile.SpawnMode;

            if (mode == ParticleSpawnMode.BurstOnly)
                return;

            var amount = mode == ParticleSpawnMode.RateOverDistance ? movedDistance : scaledDeltaTime;
            var free = particles.Length - aliveCount;
            var count = ParticleMath.TakeSpawnCount(ref accumulator, amount, profile.Rate, free);

            for (var i = 0; i < count; i++)
            {
                if (!TrySpawn())
                    return;
            }
        }

        private bool TrySpawn()
        {
            var slot = -1;

            for (var i = 0; i < particles.Length; i++)
            {
                if (particles[i].Alive || renderers[i] == null)
                    continue;

                slot = i;
                break;
            }

            if (slot < 0)
                return false;

            var origin = (Vector2)emitRoot.position + profile.EmitCenter;
            var half = profile.EmitSize * 0.5f;
            var offset = new Vector2(random.Range(-half.x, half.x), random.Range(-half.y, half.y));
            var angle = random.Range(profile.MinDirection, profile.MaxDirection);
            var speed = random.Range(profile.MinSpeed, profile.MaxSpeed);

            particles[slot] = new Particle
            {
                Alive = true,
                Age = 0f,
                Life = random.Range(profile.MinLifetime, profile.MaxLifetime),
                Position = origin + offset,
                Velocity = ParticleMath.Direction(angle) * speed,
                Size = random.Range(profile.MinSize, profile.MaxSize),
                Phase = random.NextFloat()
            };

            aliveCount++;
            return true;
        }

        private void Advance(float scaledDeltaTime)
        {
            var acceleration = profile.Acceleration;
            var constant = profile.ConstantVelocity + ExternalVelocity * profile.FlowFactor;
            var driftAmplitude = profile.DriftAmplitude;
            var driftFrequency = profile.DriftFrequency;
            var popY = SurfaceY - profile.SurfaceMargin;
            var center = (Vector2)emitRoot.position + profile.EmitCenter;

            for (var i = 0; i < particles.Length; i++)
            {
                if (!particles[i].Alive)
                    continue;

                particles[i].Age += scaledDeltaTime;

                if (particles[i].Age >= particles[i].Life)
                {
                    Kill(i);
                    continue;
                }

                particles[i].Velocity += acceleration * scaledDeltaTime;

                var drift = ParticleMath.Drift(time, particles[i].Phase, driftAmplitude, driftFrequency);
                var step = particles[i].Velocity + constant + new Vector2(drift, 0f);
                particles[i].Position += step * scaledDeltaTime;

                if (profile.PopAtSurface && particles[i].Position.y >= popY)
                {
                    Kill(i);
                    continue;
                }

                if (profile.KillOutsideBounds &&
                    ParticleMath.IsOutsideBounds(particles[i].Position, center, profile.EmitSize, profile.BoundsMargin))
                    Kill(i);
            }
        }

        private void Kill(int index)
        {
            particles[index].Alive = false;
            aliveCount--;

            if (renderers[index] != null)
                renderers[index].enabled = false;
        }

        private void Render()
        {
            var baseColor = profile.Color;
            var peak = profile.Alpha;
            var fadeIn = profile.FadeIn;
            var fadeOut = profile.FadeOut;
            var endScale = profile.EndSizeScale;

            for (var i = 0; i < particles.Length; i++)
            {
                var renderer = renderers[i];

                if (renderer == null)
                    continue;

                if (!particles[i].Alive)
                {
                    if (renderer.enabled)
                        renderer.enabled = false;

                    continue;
                }

                var life01 = particles[i].Age / particles[i].Life;
                var width = particles[i].Size * ParticleMath.SizeOverLife(life01, endScale);
                var scale = ParticleMath.LocalScaleFor(width, nativeWidths[i] * rootScale);

                var target = transforms[i];
                target.position = new Vector3(particles[i].Position.x, particles[i].Position.y, target.position.z);
                target.localScale = new Vector3(scale, scale, 1f);

                baseColor.a = peak * ParticleMath.AlphaOverLife(life01, fadeIn, fadeOut);
                renderer.color = baseColor;

                if (!renderer.enabled)
                    renderer.enabled = true;
            }
        }
    }
}
