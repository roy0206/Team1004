using System;

namespace Game.Boss
{
    public sealed class BossTimer
    {
        private float duration;

        public BossTimer(float duration)
        {
            Duration = duration;
        }

        public float Duration
        {
            get => duration;
            set => duration = value < 0f ? 0f : value;
        }

        public float Elapsed { get; private set; }
        public float Remaining => Elapsed < duration ? duration - Elapsed : 0f;
        public float Remaining01 => duration > 0f ? Remaining / duration : 0f;
        public float Progress01 => duration > 0f ? (Elapsed < duration ? Elapsed / duration : 1f) : 1f;
        public bool IsRunning { get; private set; }
        public bool IsExpired => Elapsed >= duration;

        public event Action Expired;

        public void Start()
        {
            Elapsed = 0f;
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void Reset()
        {
            Elapsed = 0f;
            IsRunning = false;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning || IsExpired)
                return;

            Elapsed += deltaTime;

            if (!IsExpired)
                return;

            Elapsed = duration;
            IsRunning = false;
            Expired?.Invoke();
        }
    }
}
