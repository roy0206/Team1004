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

        [Header("Optimization")]
        public float simulationDistance;
        public float nodePerUnit;
    }
}
