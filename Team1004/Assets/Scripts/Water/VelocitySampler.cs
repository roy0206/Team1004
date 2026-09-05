using UnityEngine;

namespace Game.Water
{
    public sealed class VelocitySampler
    {
        private float smoothing;
        private Vector2 last;
        private bool hasLast;

        public VelocitySampler()
            : this(0f)
        {
        }

        public VelocitySampler(float smoothing)
        {
            Smoothing = smoothing;
        }

        public Vector2 Velocity { get; private set; }
        public float Horizontal => Velocity.x;
        public float Vertical => Velocity.y;
        public bool HasSample => hasLast;
        public Vector2 Position => last;

        public float Smoothing
        {
            get => smoothing;
            set => smoothing = Mathf.Clamp(value, 0f, 0.99f);
        }

        public Vector2 Sample(Vector2 position, float deltaTime)
        {
            if (!hasLast)
            {
                hasLast = true;
                last = position;
                Velocity = Vector2.zero;
                return Velocity;
            }

            if (deltaTime <= 0f)
                return Velocity;

            var raw = (position - last) / deltaTime;
            last = position;
            Velocity = smoothing > 0f ? Vector2.Lerp(raw, Velocity, smoothing) : raw;
            return Velocity;
        }

        public Vector2 Sample(float x, float y, float deltaTime)
        {
            return Sample(new Vector2(x, y), deltaTime);
        }

        public void Reset()
        {
            hasLast = false;
            last = Vector2.zero;
            Velocity = Vector2.zero;
        }

        public void Reset(Vector2 position)
        {
            hasLast = true;
            last = position;
            Velocity = Vector2.zero;
        }
    }
}
