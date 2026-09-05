using UnityEngine;

namespace Game.View
{
    public sealed class CameraPanModule : Module
    {
        public const float DefaultMaxOffset = 0.8f;

        private float maxOffset = DefaultMaxOffset;

        private Vector2 offset;
        private Vector2 from;
        private Vector2 to;
        private float duration;
        private float elapsed;
        private CameraEase ease = CameraEase.SineInOut;
        private bool running;

        private bool hasReturn;
        private Vector2 returnTarget;
        private float returnHold;
        private float returnDuration;
        private CameraEase returnEase = CameraEase.SineInOut;
        private float holdRemaining;
        private bool holding;

        private Transform followTarget;
        private Vector2 followWeight = Vector2.zero;
        private Vector2 followAnchor;
        private float followLerp = 6f;

        protected override ModuleTick Ticks => ModuleTick.LateUpdate;

        public Vector2 Offset => offset;
        public Vector2 Target => to;
        public bool IsRunning => running || holding || hasReturn;
        public bool FollowEnabled { get; set; }
        public float MaxOffset => maxOffset;

        public void SetLimit(float value)
        {
            maxOffset = Mathf.Max(0f, value);
            offset = Clamp(offset);
        }

        public Vector2 Clamp(Vector2 value)
        {
            return new Vector2(
                Mathf.Clamp(value.x, -maxOffset, maxOffset),
                Mathf.Clamp(value.y, -maxOffset, maxOffset));
        }

        public void SetFollow(Transform target, Vector2 weight, float lerpPerSecond = 6f)
        {
            followTarget = target;
            followWeight = weight;
            followLerp = Mathf.Max(0.01f, lerpPerSecond);
            followAnchor = target != null ? (Vector2)target.position : Vector2.zero;
        }

        public void PanTo(Vector2 target, float durationSeconds, CameraEase easing = CameraEase.SineInOut)
        {
            hasReturn = false;
            holding = false;
            StartTween(Clamp(target), durationSeconds, easing);
        }

        public void PanPulse(
            Vector2 target,
            float inDuration,
            float holdDuration,
            float outDuration,
            CameraEase easing = CameraEase.SineInOut)
        {
            PanTo(target, inDuration, easing);

            if (outDuration <= 0f)
                return;

            hasReturn = true;
            returnTarget = Vector2.zero;
            returnHold = Mathf.Max(0f, holdDuration);
            returnDuration = outDuration;
            returnEase = easing;

            if (!running)
                BeginReturn();
        }

        public void Reset()
        {
            offset = Vector2.zero;
            from = Vector2.zero;
            to = Vector2.zero;
            running = false;
            holding = false;
            hasReturn = false;
            elapsed = 0f;
            duration = 0f;
            FollowEnabled = false;
        }

        protected override void OnLateUpdate()
        {
            Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime < 0f)
                deltaTime = 0f;

            AdvanceTween(deltaTime);

            if (!FollowEnabled || followTarget == null)
                return;

            var delta = (Vector2)followTarget.position - followAnchor;
            var wanted = new Vector2(delta.x * followWeight.x, delta.y * followWeight.y);
            var blend = Mathf.Clamp01(followLerp * deltaTime);
            offset = Clamp(Vector2.Lerp(offset, offset + wanted, blend));
        }

        private void AdvanceTween(float deltaTime)
        {
            if (running)
            {
                elapsed += deltaTime;
                var t = duration > 0f ? elapsed / duration : 1f;

                if (t >= 1f)
                {
                    offset = to;
                    running = false;

                    if (hasReturn)
                        BeginReturn();
                }
                else
                {
                    offset = CameraEasing.Lerp(from, to, ease, t);
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

        private void StartTween(Vector2 target, float durationSeconds, CameraEase easing)
        {
            if (durationSeconds <= 0f)
            {
                offset = target;
                from = target;
                to = target;
                running = false;
                elapsed = 0f;
                duration = 0f;
                return;
            }

            from = offset;
            to = target;
            duration = durationSeconds;
            elapsed = 0f;
            ease = easing;
            running = true;
        }
    }
}
