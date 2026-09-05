using UnityEngine;

namespace Game.Environment
{
    public sealed class TileStripRunner
    {
        private readonly Transform[] tiles;
        private readonly float tileWidth;
        private readonly float totalWidth;

        private float offset;

        public int TileCount => tiles?.Length ?? 0;
        public float TileWidth => tileWidth;
        public float TotalWidth => totalWidth;
        public float Offset => offset;
        public bool IsUsable => TileCount > 0 && totalWidth > 0f;

        public TileStripRunner(Transform[] tiles, float tileWidth)
        {
            this.tiles = tiles;
            this.tileWidth = tileWidth;
            totalWidth = TileStrip.TotalWidth(tileWidth, TileCount);
        }

        public void Advance(float distance)
        {
            if (!IsUsable)
                return;

            offset = TileStrip.Advance(offset, distance, totalWidth);
            Apply();
        }

        public void Rewind()
        {
            if (!IsUsable)
                return;

            offset = 0f;
            Apply();
        }

        public void Apply()
        {
            if (!IsUsable)
                return;

            for (var i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];

                if (tile == null)
                    continue;

                var local = tile.localPosition;
                local.x = TileStrip.TileX(tileWidth, tiles.Length, offset, i);
                tile.localPosition = local;
            }
        }
    }
}
