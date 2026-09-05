using UnityEngine;

namespace Game.View
{
    public sealed class CameraShakeModule : Module
    {
        private const float DefaultDecay = 1.6f;
        private const float DefaultFrequency = 22f;
        private const float DefaultMaxOffset = 0.36f;
        private const float DefaultMaxRoll = 1.2f;

        private readonly float seed;

        private float trauma;
        private float configuredDecay = DefaultDecay;
        private float decayPerSecond = DefaultDecay;
        private float frequency = DefaultFrequency;
        private float maxOffset = DefaultMaxOffset;
        private float maxRoll = DefaultMaxRoll;
        private float noiseTime;

        private Vector2 punchDirection;
        private float punchStrength;
        private float punchDuration;
        private float punchElapsed;
        private bool punching;

        private Vector2 offset;
        private float roll;

        public CameraShakeModule(float seed = 0f)
        {
            this.seed = seed;
        }

        protected override ModuleTick Ticks => ModuleTick.LateUpdate;

        public float Trauma => trauma;
        public Vector2 Offset => offset;
        public float Roll => roll;
        public float MaxOffset => maxOffset;
        public float MaxRoll => maxRoll;
        public float DecayPerSecond => decayPerSecond;
        public float Frequency => frequency;
        public bool IsPunching => punching;

        public void Configure(float maxOffset, float maxRoll, float decayPerSecond, float frequency)
        {
            this.maxOffset = Mathf.Max(0f, maxOffset);
            this.maxRoll = Mathf.Max(0f, maxRoll);
            configuredDecay = Mathf.Max(0.01f, decayPerSecond);
            this.decayPerSecond = configuredDecay;
            this.frequency = Mathf.Max(0.01f, frequency);
        }

        public void AddTrauma(float amount)
        {
            if (amount <= 0f)
                return;

            trauma = ShakeMath.Clamp01(trauma + amount);
        }

        public void SetTrauma(float amount)
        {
            trauma = ShakeMath.Clamp01(amount);
        }

        public void ShakeFor(float duration, float strength)
        {
            if (duration <= 0f || strength <= 0f)
                return;

            SetTrauma(strength);
            decayPerSecond = ShakeMath.DecayRateFor(strength, duration);
        }

        public void ShakeUnits(float duration, float amplitudeUnits)
        {
            ShakeFor(duration, ShakeMath.TraumaForAmplitude(amplitudeUnits, maxOffset));
        }

        public void Punch(Vector2 direction, float strength, float duration)
        {
            if (strength <= 0f || duration <= 0f)
                return;

            var normalized = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector2.up;
            punchDirection = normalized;
            punchStrength = Mathf.Min(strength, maxOffset);
            punchDuration = duration;
            punchElapsed = 0f;
            punching = true;
        }

        public void Clear()
        {
            trauma = 0f;
            decayPerSecond = configuredDecay;
            punching = false;
            punchElapsed = 0f;
            punchStrength = 0f;
            offset = Vector2.zero;
            roll = 0f;
        }

        protected override void OnLateUpdate()
        {
            Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime < 0f)
                deltaTime = 0f;

            noiseTime += deltaTime * frequency;

            if (trauma > 0f)
                trauma = ShakeMath.Decay(trauma, decayPerSecond, deltaTime);
            else
                decayPerSecond = configuredDecay;

            var shake = ShakeMath.Offset(seed, trauma, noiseTime, maxOffset);
            roll = ShakeMath.Roll(seed, trauma, noiseTime, maxRoll);

            if (punching)
            {
                punchElapsed += deltaTime;
                var t = punchDuration > 0f ? punchElapsed / punchDuration : 1f;

                if (t >= 1f)
                {
                    punching = false;
                }
                else
                {
                    var value = ShakeMath.PunchEnvelope(t) * punchStrength;
                    shake += punchDirection * value;
                }
            }

            offset = new Vector2(
                Mathf.Clamp(shake.x, -maxOffset, maxOffset),
                Mathf.Clamp(shake.y, -maxOffset, maxOffset));
        }
    }
}
