using System.Collections.Generic;
using UnityEngine;

namespace Game.StageProgress
{
    public readonly struct StageMarker
    {
        public StageMarker(int section, float position01)
        {
            Section = section;
            Position01 = position01;
        }

        public int Section { get; }

        public float Position01 { get; }
    }

    public static class StageProgressLayout
    {
        public static float TotalDuration(IReadOnlyList<float> durations)
        {
            if (durations == null)
                return 0f;

            var total = 0f;

            for (var i = 0; i < durations.Count; i++)
                total += Mathf.Max(0f, durations[i]);

            return total;
        }

        public static void BuildMarkers(IReadOnlyList<float> durations, List<StageMarker> results)
        {
            if (results == null)
                return;

            results.Clear();

            if (durations == null || durations.Count < 2)
                return;

            var total = TotalDuration(durations);

            if (total <= 0f)
                return;

            var elapsed = 0f;

            for (var i = 0; i < durations.Count - 1; i++)
            {
                elapsed += Mathf.Max(0f, durations[i]);
                results.Add(new StageMarker(i + 1, Mathf.Clamp01(elapsed / total)));
            }
        }

        public static float Progress01(IReadOnlyList<float> durations, int section, float sectionTime)
        {
            if (durations == null || durations.Count == 0)
                return 0f;

            var total = TotalDuration(durations);

            if (total <= 0f)
                return 0f;

            var index = Mathf.Clamp(section - 1, 0, durations.Count - 1);
            var elapsed = 0f;

            for (var i = 0; i < index; i++)
                elapsed += Mathf.Max(0f, durations[i]);

            elapsed += Mathf.Clamp(sectionTime, 0f, Mathf.Max(0f, durations[index]));

            return Mathf.Clamp01(elapsed / total);
        }
    }
}
