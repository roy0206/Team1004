using UnityEngine;

namespace Game.View
{
    public sealed class AspectModule : Module
    {
        private readonly Camera camera;
        private readonly float targetAspect;

        private int lastWidth;
        private int lastHeight;

        public float TargetAspect => targetAspect;
        public Rect CurrentRect { get; private set; } = new(0f, 0f, 1f, 1f);

        public AspectModule(Camera camera, float targetAspect)
        {
            this.camera = camera;
            this.targetAspect = targetAspect > 0f ? targetAspect : 16f / 9f;
        }

        protected override ModuleTick Ticks => ModuleTick.Update;

        protected override void OnAttached()
        {
            Apply();
        }

        protected override void OnUpdate()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
                Apply();
        }

        public void Apply()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            if (camera == null || lastWidth <= 0 || lastHeight <= 0)
                return;

            var windowAspect = (float)lastWidth / lastHeight;
            Rect rect;

            if (windowAspect > targetAspect)
            {
                var width = targetAspect / windowAspect;
                rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }
            else
            {
                var height = windowAspect / targetAspect;
                rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
            }

            CurrentRect = rect;
            camera.rect = rect;
        }
    }
}
