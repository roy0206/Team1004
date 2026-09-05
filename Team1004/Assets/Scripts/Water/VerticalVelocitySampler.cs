using UnityEngine;

namespace Game.Water
{
    public sealed class VerticalVelocitySampler
    {
        private float smoothing;
        private float lastY;
        private bool hasLast;

        public VerticalVelocitySampler()
            : this(0f)
        {
        }

        public VerticalVelocitySampler(float smoothing)
        {
            Smoothing = smoothing;
        }

        public float Velocity { get; private set; }
        public bool HasSample => hasLast;

        public float Smoothing
        {
            get => smoothing;
            set => smoothing = Mathf.Clamp(value, 0f, 0.99f);
        }

        public float Sample(float y, float deltaTime)
        {
            if (!hasLast)
            {
                hasLast = true;
                lastY = y;
                Velocity = 0f;
                return Velocity;
            }

            if (deltaTime <= 0f)
                return Velocity;

            var raw = (y - lastY) / deltaTime;
            lastY = y;
            Velocity = smoothing > 0f ? Mathf.Lerp(raw, Velocity, smoothing) : raw;
            return Velocity;
        }

        public void Reset()
        {
            hasLast = false;
            lastY = 0f;
            Velocity = 0f;
        }

        public void Reset(float y)
        {
            hasLast = true;
            lastY = y;
            Velocity = 0f;
        }
    }
}
