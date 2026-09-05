using System;
using UnityEngine;

namespace Game.Ledge
{
    [Serializable]
    public sealed class LedgeScheduleEntry
    {
        [SerializeField] private int section = 1;
        [SerializeField] private float time;

        public LedgeScheduleEntry()
        {
        }

        public LedgeScheduleEntry(int section, float time)
        {
            this.section = section;
            this.time = time;
        }

        public int Section => section;
        public float Time => time < 0f ? 0f : time;
        public bool IsValid => section > 0 && Time > 0f;
    }
}
