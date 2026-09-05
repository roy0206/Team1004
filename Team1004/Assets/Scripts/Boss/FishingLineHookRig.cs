using System;
using Game.Water;
using UnityEngine;

namespace Game.Boss
{
    [Serializable]
    public sealed class FishingLineHookRig
    {
        [SerializeField] private Transform root;
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private LineRenderer line;
        [SerializeField] private BoxCollider2D body;

        private Vector2 origin;
        private float targetY;
        private bool casting;

        public Transform Root => root;
        public Transform Visual => visual;
        public SpriteRenderer Sprite => sprite;
        public LineRenderer Line => line;
        public BoxCollider2D Body => body;
        public bool IsValid => root != null;
        public bool IsCasting => casting;
        public bool IsVisible => sprite != null && sprite.enabled;
        public Vector2 Origin => origin;
        public float TargetY => targetY;

        public Vector2 SpriteSize => sprite != null && sprite.sprite != null
            ? (Vector2)sprite.sprite.bounds.size
            : Vector2.zero;

        public Vector2 SpriteCenter => sprite != null && sprite.sprite != null
            ? (Vector2)sprite.sprite.bounds.center
            : Vector2.zero;

        public Vector2 Position
        {
            get => root != null ? (Vector2)root.position : Vector2.zero;
            set
            {
                if (root == null)
                    return;

                var current = root.position;
                root.position = new Vector3(value.x, value.y, current.z);
            }
        }

        public void SetVisible(bool visible)
        {
            if (sprite != null)
                sprite.enabled = visible;

            if (line != null)
                line.enabled = visible;
        }

        public void ApplyScale(float scale)
        {
            if (visual == null)
                return;

            visual.localRotation = Quaternion.identity;
            visual.localPosition = Vector3.zero;
            visual.localScale = new Vector3(scale, scale, 1f);
        }

        public void ApplyBody(float scale)
        {
            if (body == null)
                return;

            var size = SpriteSize * scale;

            if (size.x <= 0.0001f || size.y <= 0.0001f)
                return;

            body.size = size;
            body.offset = SpriteCenter * scale;
        }

        public void ApplyLineStyle(float width, Color color)
        {
            if (line == null)
                return;

            line.useWorldSpace = true;
            line.widthMultiplier = width;
            line.startColor = color;
            line.endColor = color;
        }

        public void BeginCast(Vector2 from, float laneTargetY)
        {
            origin = from;
            targetY = laneTargetY;
            casting = true;
        }

        public void BeginReel()
        {
            origin = Position;
        }

        public void EndCast()
        {
            casting = false;
        }

        public void DrawLine(Vector2 rodTip, float sagDepth, int segments)
        {
            if (line == null || root == null)
                return;

            var count = Mathf.Clamp(segments, 2, 32);

            if (line.positionCount != count)
                line.positionCount = count;

            var attach = (Vector2)root.position;
            var z = root.position.z;

            for (var i = 0; i < count; i++)
            {
                var u = i / (float)(count - 1);
                var point = Vector2.Lerp(rodTip, attach, u);
                point.y -= sagDepth * 4f * u * (1f - u);
                line.SetPosition(i, new Vector3(point.x, point.y, z));
            }
        }

        public void NotifyTeleport()
        {
            WaterInteractor.NotifyTeleport(root);
        }
    }
}
