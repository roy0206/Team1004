using System;

namespace Game.Spawner
{
    public sealed class SectionProfile
    {
        private readonly DifficultySample[] samples;

        public SectionProfile(DifficultySample[] samples, float spawnStopProgress)
        {
            if (samples == null || samples.Length == 0)
                throw new ArgumentException("Section profile needs at least one sample.", nameof(samples));

            this.samples = (DifficultySample[])samples.Clone();
            SpawnStopProgress = Math.Clamp(spawnStopProgress, 0f, 1f);
        }

        public float SpawnStopProgress { get; }
        public int SampleCount => samples.Length;

        public float MaxSpeedMultiplier
        {
            get
            {
                var max = 0f;
                for (var i = 0; i < samples.Length; i++)
                    max = Math.Max(max, samples[i].SpeedMultiplier);

                return max;
            }
        }

        public static SectionProfile Linear(in DifficultySample start, in DifficultySample end, float spawnStopProgress)
        {
            return new SectionProfile(new[] { start, end }, spawnStopProgress);
        }

        public DifficultySample Sample(float progress)
        {
            progress = Math.Clamp(progress, 0f, 1f);
            var enabled = progress < SpawnStopProgress;

            if (samples.Length == 1)
                return samples[0].WithSpawnEnabled(enabled);

            var scaled = progress * (samples.Length - 1);
            var index = Math.Min(samples.Length - 2, (int)Math.Floor(scaled));
            var t = scaled - index;
            return DifficultySample.Lerp(samples[index], samples[index + 1], t).WithSpawnEnabled(enabled);
        }
    }
}
