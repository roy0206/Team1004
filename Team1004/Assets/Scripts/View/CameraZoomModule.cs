using UnityEngine;

namespace Game.View
{
    public sealed class CameraZoomModule : Module
    {
        public const float DefaultMinFactor = 0.8f;
        public const float DefaultMaxFactor = 1.1f;

        private float minFactor = DefaultMinFactor;
        private float maxFactor = DefaultMaxFactor;

        private float factor = 1f;
        private float from = 1f;
        private float to = 1f;
        private float duration;
        private float elapsed;
        private CameraEase ease = CameraEase.SineInOut;
        private bool running;

        private bool hasReturn;
        private float returnTarget = 1f;
        private float returnHold;
        private float returnDuration;
        private CameraEase returnEase = CameraEase.SineInOut;
        private float holdRemaining;
        private bool holding;

        protected override ModuleTick Ticks => ModuleTick.LateUpdate;

        public float Factor => factor;
        public float Target => to;
        public bool IsRunning => running || holding || hasReturn;
        public float MinFactor => minFactor;
        public float MaxFactor => maxFactor;

        public void SetLimits(float minFactor, float maxFactor)
        {
            this.minFactor = Mathf.Max(0.05f, minFactor);
            this.maxFactor = Mathf.Max(this.minFactor, maxFactor);
            factor = Clamp(factor);
            to = Clamp(to);
            from = Clamp(from);
            returnTarget = Clamp(returnTarget);
        }

        public float Clamp(float value)
        {
            return Mathf.Clamp(value, minFactor, maxFactor);
        }

        public void SetFactor(float value)
        {
            factor = Clamp(value);
            from = factor;
            to = factor;
            running = false;
            holding = false;
            hasReturn = false;
            elapsed = 0f;
            duration = 0f;
        }

        public void ZoomTo(float target, float durationSeconds, CameraEase easing = CameraEase.SineInOut)
        {
            hasReturn = false;
            holding = false;
            StartTween(Clamp(target), durationSeconds, easing);
        }

        public void ZoomPunch(
            float target,
            float inDuration,
            float holdDuration,
            float outDuration,
            CameraEase easing = CameraEase.SineInOut)
        {
            ZoomTo(target, inDuration, easing);

            if (outDuration <= 0f)
                return;

            hasReturn = true;
            returnTarget = Clamp(1f);
            returnHold = Mathf.Max(0f, holdDuration);
            returnDuration = outDuration;
            returnEase = easing;

            if (!running)
                BeginReturn();
        }

        public void Reset()
        {
            SetFactor(1f);
        }

        protected override void OnLateUpdate()
        {
            Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime < 0f)
                deltaTime = 0f;

            if (running)
            {
                elapsed += deltaTime;
                var t = duration > 0f ? elapsed / duration : 1f;

                if (t >= 1f)
                {
                    factor = to;
                    running = false;

                    if (hasReturn)
                        BeginReturn();
                }
                else
                {
                    factor = CameraEasing.Lerp(from, to, ease, t);
                }

                return;
            }

            if (!holding)
                return;

            holdRemaining -= deltaTime;

            if (holdRemaining > 0f)
                return;

            holding = false;
            StartTween(returnTarget, returnDuration, returnEase);
        }

        private void BeginReturn()
        {
            hasReturn = false;

            if (returnHold > 0f)
            {
                holding = true;
                holdRemaining = returnHold;
                return;
            }

            StartTween(returnTarget, returnDuration, returnEase);
        }

        private void StartTween(float target, float durationSeconds, CameraEase easing)
        {
            if (durationSeconds <= 0f)
            {
                factor = target;
                from = target;
                to = target;
                running = false;
                elapsed = 0f;
                duration = 0f;
                return;
            }

            from = factor;
            to = target;
            duration = durationSeconds;
            elapsed = 0f;
            ease = easing;
            running = true;
        }
    }
}
