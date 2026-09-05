using UnityEngine;

namespace Game.Ledge
{
    public sealed class LedgeHintModule : Module
    {
        private readonly Transform target;
        private readonly float bobAmplitude;
        private readonly float bobSpeed;
        private readonly float baseY;

        private float elapsed;

        public bool IsVisible { get; private set; }

        public LedgeHintModule(Transform target, float bobAmplitude, float bobSpeed)
        {
            this.target = target;
            this.bobAmplitude = bobAmplitude;
            this.bobSpeed = bobSpeed;
            baseY = target != null ? target.localPosition.y : 0f;
        }

        protected override ModuleTick Ticks => ModuleTick.Update;

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            elapsed = 0f;

            if (target == null)
                return;

            if (target.gameObject.activeSelf != visible)
                target.gameObject.SetActive(visible);

            var position = target.localPosition;
            position.y = baseY;
            target.localPosition = position;
        }

        protected override void OnUpdate()
        {
            if (!IsVisible || target == null || bobAmplitude <= 0f)
                return;

            elapsed += Time.deltaTime;

            var position = target.localPosition;
            position.y = baseY + Mathf.Sin(elapsed * bobSpeed) * bobAmplitude;
            target.localPosition = position;
        }
    }
}
