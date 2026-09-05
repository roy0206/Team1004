using System;
using UnityEngine;

namespace Game.Boss
{
    [Serializable]
    public sealed class BossPattern
    {
        [SerializeField] private string label = "Pattern";
        [SerializeField] private int laneMask;
        [SerializeField] private float weight = 1f;
        [SerializeField] private bool requiresJump;

        public BossPattern()
        {
        }

        public BossPattern(string label, int laneMask, float weight = 1f, bool requiresJump = false)
        {
            this.label = label;
            this.laneMask = laneMask;
            this.weight = weight;
            this.requiresJump = requiresJump;
        }

        public string Label => label;
        public int LaneMask => laneMask;
        public float Weight => weight;
        public bool RequiresJump => requiresJump;
        public int LaneCount => BossLanes.Count(laneMask);

        public bool Contains(int lane)
        {
            return BossLanes.Contains(laneMask, lane);
        }

        public override string ToString()
        {
            return $"{label}[mask={laneMask} w={weight}{(requiresJump ? " jump" : string.Empty)}]";
        }
    }
}
