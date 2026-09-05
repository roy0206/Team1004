using System;
using Game.Water;
using UnityEngine;

namespace Game.Boss
{
    [Serializable]
    public sealed class BearArmView
    {
        [SerializeField] private Transform root;
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private float zRotation;

        public Transform Root => root;
        public Transform Visual => visual != null ? visual : root;
        public SpriteRenderer Sprite => sprite;
        public bool IsValid => root != null;
        public bool IsVisible => sprite != null && sprite.enabled;
        public int Lane { get; private set; } = -1;

        public float X => root != null ? root.position.x : 0f;

        public void SetLane(int lane)
        {
            Lane = lane;
        }

        public void ApplyHeight(float height)
        {
            var target = Visual;

            if (target == null || height <= 0f)
                return;

            target.localRotation = Quaternion.Euler(0f, 0f, zRotation);

            var bounds = sprite != null && sprite.sprite != null ? sprite.sprite.bounds.size : Vector3.one;
            var quarterTurn = Mathf.Abs(Mathf.Sin(zRotation * Mathf.Deg2Rad)) > 0.5f;
            var visualHeight = quarterTurn ? bounds.x : bounds.y;
            var scale = visualHeight > 0f ? height / visualHeight : 1f;
            target.localScale = new Vector3(scale, scale, 1f);
        }

        public void SetVisible(bool visible)
        {
            if (sprite != null)
                sprite.enabled = visible;
        }

        public void Place(float x, float y)
        {
            if (root == null)
                return;

            var position = root.position;
            root.position = new Vector3(x, y, position.z);
            WaterInteractor.NotifyTeleport(root);
        }

        public void SetX(float x)
        {
            if (root == null)
                return;

            var position = root.position;
            root.position = new Vector3(x, position.y, position.z);
        }
    }
}
