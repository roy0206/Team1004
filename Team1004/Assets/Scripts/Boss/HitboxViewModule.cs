using Game.Config;
using UnityEngine;

namespace Game.Boss
{
    public sealed class HitboxViewModule : Module
    {
        private const float MinimumScale = 0.0001f;

        private readonly SpriteRenderer frame;
        private readonly BoxCollider2D box;
        private readonly Color color;
        private bool visible;

        public HitboxViewModule(SpriteRenderer frame, BoxCollider2D box, Color color)
        {
            this.frame = frame;
            this.box = box;
            this.color = color;
        }

        public bool IsVisible => visible;

        protected override ModuleTick Ticks => ModuleTick.LateUpdate;

        public void SetVisible(bool value)
        {
            visible = value;

            if (frame == null)
                return;

            if (visible)
                Sync();

            frame.enabled = visible;
        }

        protected override void OnDetached()
        {
            visible = false;

            if (frame != null)
                frame.enabled = false;
        }

        protected override void OnLateUpdate()
        {
            if (visible)
                Sync();
        }

        private void Sync()
        {
            if (frame == null || box == null)
                return;

            var scale = box.transform.lossyScale;
            var scaleX = Mathf.Abs(scale.x) > MinimumScale ? scale.x : 1f;
            var scaleY = Mathf.Abs(scale.y) > MinimumScale ? scale.y : 1f;

            if (frame.drawMode != SpriteDrawMode.Sliced)
                frame.drawMode = SpriteDrawMode.Sliced;

            var frameTransform = frame.transform;
            frameTransform.localScale = new Vector3(1f / scaleX, 1f / scaleY, 1f);
            frameTransform.localPosition = new Vector3(box.offset.x, box.offset.y, frameTransform.localPosition.z);
            frame.size = new Vector2(box.size.x * Mathf.Abs(scaleX), box.size.y * Mathf.Abs(scaleY));

            var tint = color;
            tint.a = GameConfig.Current.BossHitboxAlpha;
            frame.color = tint;
        }
    }
}
