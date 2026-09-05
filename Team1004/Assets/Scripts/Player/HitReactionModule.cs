using UnityEngine;

namespace Game.Player
{
    public sealed class HitReactionModule : Module
    {
        private const float ShakeAmplitude = 0.08f;
        private const float ShakeFrequency = 60f;
        private const float FlickerRate = 30f;

        private readonly Transform target;
        private readonly SpriteRenderer renderer;

        private float stopRemaining;
        private float effectElapsed;
        private float effectDuration;
        private float pushDistance;
        private Vector3 origin;
        private Color originalColor;
        private AwaitableCompletionSource completion;

        public bool IsPlaying { get; private set; }

        public HitReactionModule(Transform target, SpriteRenderer renderer)
        {
            this.target = target;
            this.renderer = renderer;
        }

        protected override ModuleTick Ticks => ModuleTick.Update;

        public Awaitable PlayAsync(float stopDuration, float duration, float distance)
        {
            if (IsPlaying)
                return completion.Awaitable;

            completion = new AwaitableCompletionSource();
            IsPlaying = true;
            stopRemaining = Mathf.Max(0f, stopDuration);
            effectDuration = Mathf.Max(0.01f, duration);
            pushDistance = Mathf.Max(0f, distance);
            effectElapsed = 0f;
            origin = target != null ? target.position : Vector3.zero;

            if (renderer != null)
            {
                originalColor = renderer.color;
                renderer.color = Color.white;
            }

            return completion.Awaitable;
        }

        protected override void OnUpdate()
        {
            if (!IsPlaying)
                return;

            if (stopRemaining > 0f)
            {
                stopRemaining -= Time.deltaTime;
                return;
            }

            effectElapsed += Time.deltaTime;
            var t = Mathf.Clamp01(effectElapsed / effectDuration);
            var push = -pushDistance * Mathf.SmoothStep(0f, 1f, t);
            var shake = Mathf.Sin(effectElapsed * ShakeFrequency) * ShakeAmplitude * (1f - t);

            if (target != null)
                target.position = origin + new Vector3(push, shake, 0f);

            if (renderer != null)
                renderer.color = Mathf.FloorToInt(effectElapsed * FlickerRate) % 2 == 0 ? Color.white : originalColor;

            if (t < 1f)
                return;

            if (target != null)
                target.position = origin + new Vector3(push, 0f, 0f);

            Finish();
        }

        protected override void OnDetached()
        {
            if (IsPlaying)
                Finish();
        }

        private void Finish()
        {
            IsPlaying = false;

            if (renderer != null)
                renderer.color = originalColor;

            var source = completion;
            completion = null;
            source?.TrySetResult();
        }
    }
}
