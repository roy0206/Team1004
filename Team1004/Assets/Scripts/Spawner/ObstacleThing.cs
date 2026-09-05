using System;
using Game.Animation;
using Game.Particles;
using Game.Player;
using UnityEngine;

namespace Game.Spawner
{
    public sealed class ObstacleThing : Hazard, IPooledObject
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite[] variants = Array.Empty<Sprite>();
        [SerializeField] private CustomAnimation swimClip;
        [SerializeField] private BubbleTrailThing trail;

        private SpriteAnimatorModule animator;

        public ObstacleRuntimeModule Runtime => GetModule<ObstacleRuntimeModule>();

        public SpriteAnimatorModule Animator => animator;

        public CustomAnimation SwimClip => swimClip;

        public bool HasSwimClip => swimClip != null;

        public int VariantCount => variants != null ? variants.Length : 0;

        public void ApplyVariant(uint variantSeed)
        {
            if (visual == null || variants == null || variants.Length == 0)
                return;

            var sprite = variants[ObstacleVisual.PickVariant(variantSeed, variants.Length)];

            if (sprite != null)
                visual.sprite = sprite;
        }

        public BubbleTrailThing Trail => trail;

        public void OnSpawned()
        {
            ClearModules();
            animator = null;
            PlaySwimClip();

            if (trail == null)
                return;

            trail.Clear();
            trail.SetEmitting(true);
        }

        public void OnReleased()
        {
            StopSwimClip();
            ClearModules();

            if (trail == null)
                return;

            trail.SetEmitting(false);
            trail.Clear();
        }

        private void PlaySwimClip()
        {
            if (swimClip == null || visual == null)
                return;

            animator = AddModule(new SpriteAnimatorModule(visual));
            animator.Play(swimClip);
        }

        private void StopSwimClip()
        {
            if (animator == null)
                return;

            animator.Stop();
            animator = null;
        }
    }
}
