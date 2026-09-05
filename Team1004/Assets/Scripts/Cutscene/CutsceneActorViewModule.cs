using UnityEngine;

namespace Game.Cutscene
{
    public sealed class CutsceneActorViewModule : Module
    {
        private readonly GameObject target;
        private readonly SpriteRenderer spriteRenderer;

        public CutsceneActorViewModule(GameObject target, SpriteRenderer spriteRenderer)
        {
            this.target = target;
            this.spriteRenderer = spriteRenderer;
        }

        protected override ModuleTick Ticks => ModuleTick.None;

        public bool HasRenderer => spriteRenderer != null;

        public bool IsVisible => spriteRenderer != null ? spriteRenderer.enabled : target.activeSelf;

        public float Alpha
        {
            get => spriteRenderer != null ? spriteRenderer.color.a : 1f;
            set
            {
                if (spriteRenderer == null)
                    return;

                var color = spriteRenderer.color;
                color.a = Mathf.Clamp01(value);
                spriteRenderer.color = color;
            }
        }

        public Color Color
        {
            get => spriteRenderer != null ? spriteRenderer.color : Color.white;
            set
            {
                if (spriteRenderer != null)
                    spriteRenderer.color = value;
            }
        }

        public void SetVisible(bool visible)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = visible;
            else
                target.SetActive(visible);
        }

        public void SetSprite(Sprite sprite)
        {
            if (spriteRenderer != null)
                spriteRenderer.sprite = sprite;
        }
    }
}
