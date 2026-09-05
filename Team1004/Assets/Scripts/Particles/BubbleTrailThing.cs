using System.Collections.Generic;
using Game.Config;
using UnityEngine;

namespace Game.Particles
{
    public sealed class BubbleTrailThing : MonoThing
    {
        [SerializeField] private ParticleProfile profile;
        [SerializeField] private Transform particleRoot;
        [SerializeField] private bool emitOnAwake = true;

        private SpriteEmitterModule emitter;
        private bool isInitialized;

        public bool IsInitialized => isInitialized;
        public SpriteEmitterModule Emitter => emitter;
        public ParticleProfile Profile => profile;
        public int Capacity => emitter != null ? emitter.Capacity : 0;
        public int AliveCount => emitter != null ? emitter.AliveCount : 0;
        public bool IsEmitting => emitter != null && emitter.IsEmitting;

        public float SpeedScale
        {
            get => emitter != null ? emitter.SpeedScale : 1f;
            set
            {
                if (emitter != null)
                    emitter.SpeedScale = value;
            }
        }

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            var root = particleRoot != null ? particleRoot : transform;
            var buffer = new List<SpriteRenderer>();

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var renderer = child.GetComponent<SpriteRenderer>();

                if (renderer != null)
                    buffer.Add(renderer);
            }

            var seed = unchecked((uint)GetInstanceID()) * 2654435761u + 1u;
            emitter = AddModule(new SpriteEmitterModule(transform, profile, buffer.ToArray(), seed));
            emitter.IsEmitting = emitOnAwake;
            emitter.SurfaceY = GameConfig.Current.WaterSurfaceY;
        }

        public void SetEmitting(bool emitting)
        {
            Initialize();
            emitter.IsEmitting = emitting;
        }

        public void SetSurfaceY(float surfaceY)
        {
            Initialize();
            emitter.SurfaceY = surfaceY;
        }

        public void Burst(int count)
        {
            Initialize();
            emitter.Burst(count);
        }

        public void BurstSmall()
        {
            Initialize();

            if (profile != null)
                emitter.Burst(profile.BurstSmall);
        }

        public void BurstLarge()
        {
            Initialize();

            if (profile != null)
                emitter.Burst(profile.BurstLarge);
        }

        public void Clear()
        {
            Initialize();
            emitter.Clear();
            emitter.ResetMovementSample();
        }
    }
}
