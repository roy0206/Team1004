using UnityEngine;

namespace Game.View
{
    [CreateAssetMenu(menuName = "Team1004/Camera Fx Settings", fileName = "CameraFxSettings")]
    public sealed class CameraFxSettings : ScriptableObject
    {
        [SerializeField] private float minZoomIn = 0.8f;
        [SerializeField] private float maxZoomOut = 1.1f;
        [SerializeField] private float shakeMaxOffset = 0.36f;
        [SerializeField] private float shakeMaxRoll = 1.2f;
        [SerializeField] private float shakeDecayPerSecond = 1.6f;
        [SerializeField] private float shakeFrequency = 22f;
        [SerializeField] private float panMaxOffset = 0.8f;

        public float MinZoomIn => Mathf.Max(0.05f, minZoomIn);
        public float MaxZoomOut => Mathf.Max(MinZoomIn, maxZoomOut);
        public float ShakeMaxOffset => Mathf.Max(0f, shakeMaxOffset);
        public float ShakeMaxRoll => Mathf.Max(0f, shakeMaxRoll);
        public float ShakeDecayPerSecond => Mathf.Max(0.01f, shakeDecayPerSecond);
        public float ShakeFrequency => Mathf.Max(0.01f, shakeFrequency);
        public float PanMaxOffset => Mathf.Max(0f, panMaxOffset);
    }
}
