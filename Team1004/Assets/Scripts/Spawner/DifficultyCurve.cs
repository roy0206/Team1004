using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Spawner
{
    [Serializable]
    public sealed class SectionDifficulty
    {
        [SerializeField] private AnimationCurve speedMultiplier = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [SerializeField] private AnimationCurve patternGap = AnimationCurve.Linear(0f, 2f, 1f, 2f);
        [SerializeField] private AnimationCurve minReactionMargin = AnimationCurve.Linear(0f, 0.6f, 1f, 0.6f);
        [SerializeField] private AnimationCurve jumpRequiredRatio = AnimationCurve.Linear(0f, 0.25f, 1f, 0.25f);
        [SerializeField] private AnimationCurve tier1Weight = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [SerializeField] private AnimationCurve tier2Weight = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [SerializeField] private AnimationCurve tier3Weight = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float spawnStopProgress = 1f;

        public SectionDifficulty()
        {
        }

        public SectionDifficulty(in DifficultySample start, in DifficultySample end, float spawnStopProgress)
        {
            speedMultiplier = AnimationCurve.Linear(0f, start.SpeedMultiplier, 1f, end.SpeedMultiplier);
            patternGap = AnimationCurve.Linear(0f, start.PatternGap, 1f, end.PatternGap);
            minReactionMargin = AnimationCurve.Linear(0f, start.MinReactionMargin, 1f, end.MinReactionMargin);
            jumpRequiredRatio = AnimationCurve.Linear(0f, start.JumpRequiredRatio, 1f, end.JumpRequiredRatio);
            tier1Weight = AnimationCurve.Linear(0f, start.Tier1Weight, 1f, end.Tier1Weight);
            tier2Weight = AnimationCurve.Linear(0f, start.Tier2Weight, 1f, end.Tier2Weight);
            tier3Weight = AnimationCurve.Linear(0f, start.Tier3Weight, 1f, end.Tier3Weight);
            this.spawnStopProgress = Mathf.Clamp01(spawnStopProgress);
        }

        public float SpawnStopProgress => spawnStopProgress;

        public DifficultySample Sample(float progress)
        {
            progress = Mathf.Clamp01(progress);
            return new DifficultySample(
                Evaluate(speedMultiplier, progress, 1f),
                Evaluate(patternGap, progress, 2f),
                Evaluate(minReactionMargin, progress, 0.6f),
                Evaluate(jumpRequiredRatio, progress, 0.25f),
                Evaluate(tier1Weight, progress, 1f),
                Evaluate(tier2Weight, progress, 1f),
                Evaluate(tier3Weight, progress, 1f),
                progress < spawnStopProgress);
        }

        public SectionProfile ToProfile(int sampleCount = 21)
        {
            sampleCount = Mathf.Max(2, sampleCount);
            var samples = new DifficultySample[sampleCount];

            for (var i = 0; i < sampleCount; i++)
                samples[i] = Sample(i / (float)(sampleCount - 1));

            return new SectionProfile(samples, spawnStopProgress);
        }

        private static float Evaluate(AnimationCurve curve, float t, float fallback)
        {
            return curve == null || curve.length == 0 ? fallback : curve.Evaluate(t);
        }
    }

    [CreateAssetMenu(fileName = "DifficultyCurve", menuName = "Team1004/Spawner/Difficulty Curve")]
    public sealed class DifficultyCurve : ScriptableObject
    {
        [SerializeField] private SectionDifficulty[] sections =
        {
            new SectionDifficulty(),
            new SectionDifficulty(),
            new SectionDifficulty(),
            new SectionDifficulty()
        };

        public IReadOnlyList<SectionDifficulty> Sections => sections;

        public DifficultyProfile ToProfile()
        {
            var profiles = new List<SectionProfile>();

            if (sections != null)
                for (var i = 0; i < sections.Length; i++)
                    profiles.Add((sections[i] ?? new SectionDifficulty()).ToProfile());

            if (profiles.Count == 0)
                profiles.Add(new SectionDifficulty().ToProfile());

            return new DifficultyProfile(profiles);
        }

#if UNITY_EDITOR
        public void EditorInitialize(SectionDifficulty[] sections)
        {
            this.sections = sections ?? new[] { new SectionDifficulty() };
        }
#endif
    }
}
