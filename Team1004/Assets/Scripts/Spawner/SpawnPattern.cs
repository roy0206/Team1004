using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Spawner
{
    [Serializable]
    public sealed class SpawnPatternEntry
    {
        [SerializeField] private ObstacleDefinition obstacle;
        [SerializeField] private Lane startLane = Lane.Middle;
        [SerializeField, Min(0f)] private float arrivalOffset;

        public SpawnPatternEntry()
        {
        }

        public SpawnPatternEntry(ObstacleDefinition obstacle, Lane startLane, float arrivalOffset)
        {
            this.obstacle = obstacle;
            this.startLane = startLane;
            this.arrivalOffset = Mathf.Max(0f, arrivalOffset);
        }

        public ObstacleDefinition Obstacle => obstacle;
        public Lane StartLane => startLane;
        public float ArrivalOffset => arrivalOffset;
    }

    [CreateAssetMenu(fileName = "Pattern", menuName = "Team1004/Spawner/Spawn Pattern")]
    public sealed class SpawnPattern : ScriptableObject
    {
        [SerializeField] private string id = "Pattern";
        [SerializeField] private List<SpawnPatternEntry> entries = new List<SpawnPatternEntry>();
        [SerializeField] private bool manualJumpRequired;
        [SerializeField] private bool enabled = true;

        public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
        public IReadOnlyList<SpawnPatternEntry> Entries => entries;
        public bool ManualJumpRequired => manualJumpRequired;
        public bool Enabled => enabled;

        public PatternSpec ToSpec(Dictionary<ObstacleDefinition, ObstacleSpec> specCache)
        {
            var list = new List<PatternEntrySpec>(entries.Count);

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.Obstacle == null)
                    return null;

                if (specCache == null)
                {
                    list.Add(new PatternEntrySpec(entry.Obstacle.ToSpec(), (int)entry.StartLane, entry.ArrivalOffset));
                    continue;
                }

                if (!specCache.TryGetValue(entry.Obstacle, out var spec))
                {
                    spec = entry.Obstacle.ToSpec();
                    specCache.Add(entry.Obstacle, spec);
                }

                list.Add(new PatternEntrySpec(spec, (int)entry.StartLane, entry.ArrivalOffset));
            }

            return new PatternSpec(Id, list, manualJumpRequired);
        }

#if UNITY_EDITOR
        public void EditorInitialize(string id, List<SpawnPatternEntry> entries, bool manualJumpRequired)
        {
            this.id = id;
            this.entries = entries ?? new List<SpawnPatternEntry>();
            this.manualJumpRequired = manualJumpRequired;
            enabled = true;
        }
#endif
    }
}
