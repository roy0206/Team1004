using UnityEngine;

namespace Game.View
{
    public static class ShakeMath
    {
        public const float ChannelStride = 17.31f;
        public const float PunchOscillations = 1.5f;

        public static float Decay(float trauma, float decayPerSecond, float deltaTime)
        {
            var next = trauma - decayPerSecond * deltaTime;
            return next < 0f ? 0f : next > 1f ? 1f : next;
        }

        public static float Clamp01(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }

        public static float Amplitude(float trauma)
        {
            var t = Clamp01(trauma);
            return t * t;
        }

        public static float Noise(float seed, int channel, float time)
        {
            return Mathf.PerlinNoise(seed + channel * ChannelStride, time) * 2f - 1f;
        }

        public static Vector2 Offset(float seed, float trauma, float time, float maxOffset)
        {
            var amplitude = Amplitude(trauma) * maxOffset;
            return new Vector2(Noise(seed, 0, time) * amplitude, Noise(seed, 1, time) * amplitude);
        }

        public static float Roll(float seed, float trauma, float time, float maxRoll)
        {
            return Noise(seed, 2, time) * Amplitude(trauma) * maxRoll;
        }

        public static float PunchEnvelope(float t)
        {
            var x = Clamp01(t);
            return Mathf.Cos(x * Mathf.PI * PunchOscillations) * (1f - x);
        }

        public static float TraumaForAmplitude(float amplitude, float maxOffset)
        {
            if (maxOffset <= 0f || amplitude <= 0f)
                return 0f;

            return Mathf.Sqrt(Clamp01(amplitude / maxOffset));
        }

        public static float DecayRateFor(float trauma, float duration)
        {
            if (duration <= 0f)
                return float.MaxValue;

            return Clamp01(trauma) / duration;
        }
    }
}
