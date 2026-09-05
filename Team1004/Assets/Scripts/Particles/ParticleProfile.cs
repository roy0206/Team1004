using UnityEngine;

namespace Game.Particles
{
    [CreateAssetMenu(menuName = "Team1004/Particles/Particle Profile", fileName = "ParticleProfile")]
    public sealed class ParticleProfile : ScriptableObject
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private int sortingOrder = -4;

        [SerializeField] private ParticleSpawnMode spawnMode = ParticleSpawnMode.RateOverTime;
        [SerializeField] private float rate = 4f;
        [SerializeField] private Vector2 emitCenter = Vector2.zero;
        [SerializeField] private Vector2 emitSize = Vector2.zero;

        [SerializeField] private Vector2 lifetimeRange = new(0.6f, 1f);
        [SerializeField] private Vector2 speedRange = new(0.1f, 0.3f);
        [SerializeField] private Vector2 directionRange = new(80f, 100f);
        [SerializeField] private Vector2 sizeRange = new(0.05f, 0.1f);

        [SerializeField] private float endSizeScale = 1f;
        [SerializeField] private float alpha = 0.5f;
        [SerializeField] private float fadeIn = 0.15f;
        [SerializeField] private float fadeOut = 0.35f;

        [SerializeField] private Vector2 acceleration = Vector2.zero;
        [SerializeField] private Vector2 constantVelocity = Vector2.zero;
        [SerializeField] private float flowFactor;
        [SerializeField] private float driftAmplitude;
        [SerializeField] private float driftFrequency = 1f;

        [SerializeField] private bool popAtSurface;
        [SerializeField] private float surfaceMargin;
        [SerializeField] private bool killOutsideBounds;
        [SerializeField] private float boundsMargin = 1f;

        [SerializeField] private int burstSmall;
        [SerializeField] private int burstLarge;

        public Sprite Sprite => sprite;
        public Color Color => color;
        public int SortingOrder => sortingOrder;

        public ParticleSpawnMode SpawnMode => spawnMode;
        public float Rate => Mathf.Max(0f, rate);
        public Vector2 EmitCenter => emitCenter;
        public Vector2 EmitSize => emitSize;

        public float MinLifetime => Mathf.Max(0.01f, Mathf.Min(lifetimeRange.x, lifetimeRange.y));
        public float MaxLifetime => Mathf.Max(MinLifetime, Mathf.Max(lifetimeRange.x, lifetimeRange.y));
        public float MinSpeed => Mathf.Min(speedRange.x, speedRange.y);
        public float MaxSpeed => Mathf.Max(speedRange.x, speedRange.y);
        public float MinDirection => Mathf.Min(directionRange.x, directionRange.y);
        public float MaxDirection => Mathf.Max(directionRange.x, directionRange.y);
        public float MinSize => Mathf.Max(0.001f, Mathf.Min(sizeRange.x, sizeRange.y));
        public float MaxSize => Mathf.Max(MinSize, Mathf.Max(sizeRange.x, sizeRange.y));

        public float EndSizeScale => Mathf.Max(0f, endSizeScale);
        public float Alpha => Mathf.Clamp01(alpha);
        public float FadeIn => Mathf.Clamp01(fadeIn);
        public float FadeOut => Mathf.Clamp01(fadeOut);

        public Vector2 Acceleration => acceleration;
        public Vector2 ConstantVelocity => constantVelocity;
        public float FlowFactor => flowFactor;
        public float DriftAmplitude => driftAmplitude;
        public float DriftFrequency => driftFrequency;

        public bool PopAtSurface => popAtSurface;
        public float SurfaceMargin => surfaceMargin;
        public bool KillOutsideBounds => killOutsideBounds;
        public float BoundsMargin => Mathf.Max(0f, boundsMargin);

        public int BurstSmall => Mathf.Max(0, burstSmall);
        public int BurstLarge => Mathf.Max(0, burstLarge);
    }
}
