using System;
using System.Collections.Generic;
using Game.Config;

namespace Game.Player
{
    public sealed class HurtboxModule : Module
    {
        private readonly LanePlayer player;
        private readonly HashSet<Hazard> overlapping = new();
        private readonly List<Hazard> staleBuffer = new();

        public bool HasHit { get; private set; }
        public Hazard LastHit { get; private set; }
        public int OverlapCount => overlapping.Count;

        public event Action<Hazard> Hit;

        public HurtboxModule(LanePlayer player)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
        }

        protected override ModuleTick Ticks => ModuleTick.Update;

        protected override void OnUpdate()
        {
            if (HasHit || overlapping.Count == 0 || !player.IsVulnerable || GameConfig.Current.DebugInvincible)
                return;

            Hazard first = null;
            staleBuffer.Clear();

            foreach (var hazard in overlapping)
            {
                if (hazard == null || !hazard.isActiveAndEnabled)
                {
                    staleBuffer.Add(hazard);
                    continue;
                }

                first = hazard;
                break;
            }

            for (var i = 0; i < staleBuffer.Count; i++)
                overlapping.Remove(staleBuffer[i]);

            if (first == null)
                return;

            HasHit = true;
            LastHit = first;
            Hit?.Invoke(first);
        }

        public void AddOverlap(Hazard hazard)
        {
            if (hazard != null)
                overlapping.Add(hazard);
        }

        public void RemoveOverlap(Hazard hazard)
        {
            if (hazard != null)
                overlapping.Remove(hazard);
        }

        public void ResetHit()
        {
            HasHit = false;
            LastHit = null;
        }

        protected override void OnDetached()
        {
            overlapping.Clear();
        }
    }
}
