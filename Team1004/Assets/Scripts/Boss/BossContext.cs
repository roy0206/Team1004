using System;
using Game.Player;
using UnityEngine;

namespace Game.Boss
{
    public sealed class BossContext
    {
        public BossContext(LanePlayer player, Transform scrollRoot = null, Camera camera = null)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            Player = player;
            ScrollRoot = scrollRoot;
            Camera = camera;
        }

        public LanePlayer Player { get; }
        public Transform ScrollRoot { get; }
        public Camera Camera { get; }
    }
}
