using System.Collections.Generic;
using UnityEngine;

namespace Game.Spawner
{
    [CreateAssetMenu(fileName = "PatternLibrary", menuName = "Team1004/Spawner/Pattern Library")]
    public sealed class PatternLibrary : ScriptableObject
    {
        [SerializeField] private List<SpawnPattern> patterns = new List<SpawnPattern>();

        public IReadOnlyList<SpawnPattern> Patterns => patterns;

        public int BuildSpecs(List<PatternSpec> output)
        {
            output.Clear();
            var cache = new Dictionary<ObstacleDefinition, ObstacleSpec>();

            for (var i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];
                if (pattern == null || !pattern.Enabled)
                    continue;

                var spec = pattern.ToSpec(cache);
                if (spec == null)
                {
                    Debug.LogWarning("[PatternLibrary] Pattern has an empty obstacle slot and was skipped: '" + pattern.name + "'.", pattern);
                    continue;
                }

                output.Add(spec);
            }

            return output.Count;
        }

        public int CollectObstacles(List<ObstacleDefinition> output)
        {
            output.Clear();

            for (var i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];
                if (pattern == null)
                    continue;

                var entries = pattern.Entries;
                for (var j = 0; j < entries.Count; j++)
                {
                    var obstacle = entries[j]?.Obstacle;
                    if (obstacle != null && !output.Contains(obstacle))
                        output.Add(obstacle);
                }
            }

            return output.Count;
        }

#if UNITY_EDITOR
        public void EditorInitialize(List<SpawnPattern> patterns)
        {
            this.patterns = patterns ?? new List<SpawnPattern>();
        }
#endif
    }
}
