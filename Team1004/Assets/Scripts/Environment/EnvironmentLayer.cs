using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Environment
{
    [Serializable]
    public sealed class EnvironmentLayer
    {
        [SerializeField] private Transform root;
        [SerializeField] private float tileWidth = 8f;
        [SerializeField] private float speedScale = 1f;

        public Transform Root => root;
        public float TileWidth => tileWidth;
        public float SpeedScale => speedScale;

        public TileStripRunner CreateRunner(List<Transform> buffer)
        {
            if (root == null || buffer == null)
                return new TileStripRunner(Array.Empty<Transform>(), tileWidth);

            buffer.Clear();

            for (var i = 0; i < root.childCount; i++)
                buffer.Add(root.GetChild(i));

            return new TileStripRunner(buffer.ToArray(), tileWidth);
        }
    }
}
