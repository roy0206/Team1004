using UnityEngine;

namespace Game.Ledge
{
    [CreateAssetMenu(menuName = "Team1004/Ledge Data", fileName = "LedgeData")]
    public sealed class LedgeData : ScriptableObject
    {
        [SerializeField] private LedgeScheduleEntry[] ledgeEntries =
        {
            new(1, 5f),
            new(1, 12.5f),
            new(2, 15f)
        };

        [SerializeField] private int hintSection = 1;
        [SerializeField] private float approachSafeTime = 2f;
        [SerializeField] private float stepHeight = 1.1f;
        [SerializeField] private float cameraMoveDuration = 0.7f;
        [SerializeField] private float spawnX = 7.5f;
        [SerializeField] private float retireX = -20f;
        [SerializeField] private float clearMargin = 1.2f;
        [SerializeField] private bool instantFail;
        [SerializeField] private float environmentRiseAmount = 0.35f;
        [SerializeField] private float environmentSettleDuration = 1.2f;
        [SerializeField] private float sectionEndMargin = 3f;

        public int EntryCount => ledgeEntries?.Length ?? 0;
        public int HintSection => hintSection;
        public float ApproachSafeTime => approachSafeTime;
        public float StepHeight => stepHeight;
        public float CameraMoveDuration => cameraMoveDuration;
        public float SpawnX => spawnX;
        public float RetireX => retireX;
        public float ClearMargin => clearMargin;
        public bool InstantFail => instantFail;
        public float EnvironmentRiseAmount => environmentRiseAmount;
        public float EnvironmentSettleDuration => environmentSettleDuration;
        public float SectionEndMargin => sectionEndMargin;

        public int HintEntryIndex
        {
            get
            {
                for (var i = 0; i < EntryCount; i++)
                {
                    var entry = GetEntry(i);

                    if (entry != null && entry.IsValid && entry.Section == hintSection)
                        return i;
                }

                return -1;
            }
        }

        public LedgeScheduleEntry GetEntry(int index)
        {
            if (ledgeEntries == null || index < 0 || index >= ledgeEntries.Length)
                return null;

            return ledgeEntries[index];
        }

        public int CountForSection(int section)
        {
            var count = 0;

            for (var i = 0; i < EntryCount; i++)
            {
                var entry = GetEntry(i);

                if (entry != null && entry.IsValid && entry.Section == section)
                    count++;
            }

            return count;
        }

        public float GetLedgeTime(int section)
        {
            var best = 0f;

            for (var i = 0; i < EntryCount; i++)
            {
                var entry = GetEntry(i);

                if (entry == null || !entry.IsValid || entry.Section != section)
                    continue;

                if (best <= 0f || entry.Time < best)
                    best = entry.Time;
            }

            return best;
        }

        public bool HasLedge(int section)
        {
            return CountForSection(section) > 0;
        }
    }
}
