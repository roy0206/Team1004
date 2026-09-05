using UnityEngine;

namespace Game.Water
{
    [CreateAssetMenu(fileName = "WaterSettings", menuName = "ScriptableObjects/WaterSettings")]
    public class WaterSettings : ScriptableObject
    {
        static WaterSettings _currentSettings;
        public static WaterSettings currentSettings =>
            _currentSettings ??= Resources.Load<WaterSettings>("WaterSettings");

        [Header("Physics")]
        public float tension;
        public float damping;
        public float spread;
        public int iterationsPerFrame = 1;

        [Header("Collision")]
        public float surfaceCollisionDistance;
        public float collisionVelocityTransfer;
        [Range(0f, 0.99f)]
        public float velocitySmoothing;

        [SerializeField] private float maxInjectedVelocity = 1.5f;
        [SerializeField] private int injectionWarmupFrames = 3;
        [SerializeField] private float teleportDistance = 1f;

        [Header("Wake")]
        public float wakeVelocityTransfer = 0.15f;
        public float wakeInfluenceDistance = 1f;

        public float MaxInjectedVelocity
        {
            get => maxInjectedVelocity;
            set => maxInjectedVelocity = Mathf.Max(0f, value);
        }

        public int InjectionWarmupFrames
        {
            get => injectionWarmupFrames;
            set => injectionWarmupFrames = Mathf.Max(0, value);
        }

        public float TeleportDistance
        {
            get => teleportDistance;
            set => teleportDistance = Mathf.Max(0f, value);
        }

        [Header("Optimization")]
        public float simulationDistance;
        public float nodePerUnit;
    }
}
