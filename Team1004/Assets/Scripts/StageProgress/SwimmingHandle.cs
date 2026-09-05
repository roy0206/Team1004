using UnityEngine;
using UnityEngine.UI;

namespace Game.StageProgress
{
    [DisallowMultipleComponent]
    public sealed class SwimmingHandle : MonoBehaviour
    {
        private const float Tau = Mathf.PI * 2f;

        [SerializeField] private RectTransform body;
        [SerializeField] private Image target;
        [SerializeField] private Sprite[] frames;

        [SerializeField] private float framesPerSecond = 9f;
        [SerializeField] private float bobAmplitude = 4.5f;
        [SerializeField] private float bobFrequency = 2.3f;
        [SerializeField] private float tiltDegrees = 9f;
        [SerializeField] private float tiltFrequency = 2.3f;
        [SerializeField] private float surgeAmount = 0.09f;
        [SerializeField, Range(0f, 1f)] private float restRate = 0.45f;
        [SerializeField] private float speedSharpness = 6f;

        private Vector2 basePosition;
        private Vector3 baseScale = Vector3.one;
        private bool initialized;
        private float frameTimer;
        private int frameIndex;
        private float waveTime;
        private float speed01;
        private float speedTarget;

        public void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            if (body != null)
            {
                basePosition = body.anchoredPosition;
                baseScale = body.localScale;
            }

            frameIndex = 0;
            ApplyFrame();
        }

        public void SetSpeed01(float value)
        {
            speedTarget = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            if (body == null)
                return;

            var delta = Time.deltaTime;

            speed01 = Mathf.Lerp(speed01, speedTarget, 1f - Mathf.Exp(-speedSharpness * delta));

            var rate = Mathf.Lerp(restRate, 1f, speed01);

            waveTime += delta * rate;

            AdvanceFrames(delta * rate);

            var bob = Mathf.Sin(waveTime * bobFrequency * Tau) * bobAmplitude;
            var tilt = Mathf.Sin(waveTime * tiltFrequency * Tau + Mathf.PI * 0.5f) * tiltDegrees;
            var surge = 1f + Mathf.Sin(waveTime * bobFrequency * Tau) * surgeAmount * speed01;

            body.anchoredPosition = new Vector2(basePosition.x, basePosition.y + bob);
            body.localRotation = Quaternion.Euler(0f, 0f, tilt);
            body.localScale = new Vector3(baseScale.x * surge, baseScale.y, baseScale.z);
        }

        private void AdvanceFrames(float scaledDelta)
        {
            if (frames == null || frames.Length < 2 || framesPerSecond <= 0f)
                return;

            frameTimer += scaledDelta * framesPerSecond;

            if (frameTimer < 1f)
                return;

            var steps = Mathf.FloorToInt(frameTimer);

            frameTimer -= steps;
            frameIndex = (frameIndex + steps) % frames.Length;

            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (target == null || frames == null || frames.Length == 0)
                return;

            target.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
        }
    }
}
