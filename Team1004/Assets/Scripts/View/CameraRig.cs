using UnityEngine;

namespace Game.View
{
    public sealed class CameraRig : Module
    {
        private readonly Camera camera;
        private readonly Transform target;
        private readonly CameraShakeModule shake;
        private readonly CameraZoomModule zoom;
        private readonly CameraPanModule pan;

        private Vector3 homePosition;
        private float homeSize;
        private float homeRoll;

        private Vector3 basePosition;
        private float baseSize;
        private float baseRoll;
        private bool rollApplied;

        public CameraRig(
            Camera camera,
            Transform target,
            CameraShakeModule shake,
            CameraZoomModule zoom,
            CameraPanModule pan)
        {
            this.camera = camera;
            this.target = target;
            this.shake = shake;
            this.zoom = zoom;
            this.pan = pan;
        }

        protected override ModuleTick Ticks => ModuleTick.LateUpdate;

        public Camera Camera => camera;
        public CameraShakeModule Shake => shake;
        public CameraZoomModule Zoom => zoom;
        public CameraPanModule Pan => pan;
        public Vector3 HomePosition => homePosition;
        public float HomeSize => homeSize;

        public Vector3 BasePosition
        {
            get => basePosition;
            set => basePosition = value;
        }

        public float BaseSize
        {
            get => baseSize;
            set => baseSize = Mathf.Max(CameraComposition.MinSize, value);
        }

        public float BaseRoll
        {
            get => baseRoll;
            set => baseRoll = value;
        }

        public float VisibleHalfHeight => CameraComposition.VisibleHalfHeight(baseSize, zoom != null ? zoom.Factor : 1f);

        protected override void OnAttached()
        {
            homePosition = target != null ? target.position : Vector3.zero;
            homeSize = camera != null ? camera.orthographicSize : 5f;
            homeRoll = target != null ? target.eulerAngles.z : 0f;
            basePosition = homePosition;
            baseSize = homeSize;
            baseRoll = homeRoll;
        }

        public void ApplySettings(CameraFxSettings settings)
        {
            if (settings == null)
                return;

            shake?.Configure(
                settings.ShakeMaxOffset,
                settings.ShakeMaxRoll,
                settings.ShakeDecayPerSecond,
                settings.ShakeFrequency);
            zoom?.SetLimits(settings.MinZoomIn, settings.MaxZoomOut);
            pan?.SetLimit(settings.PanMaxOffset);
        }

        public void CaptureBase()
        {
            if (target != null)
                basePosition = target.position;

            if (camera != null)
                baseSize = camera.orthographicSize;
        }

        public void SetBase(Vector3 position, float size)
        {
            basePosition = position;
            baseSize = Mathf.Max(CameraComposition.MinSize, size);
        }

        public void ResetEffects()
        {
            shake?.Clear();
            zoom?.Reset();
            pan?.Reset();
            Apply();
        }

        public void ResetAll()
        {
            basePosition = homePosition;
            baseSize = homeSize;
            baseRoll = homeRoll;
            ResetEffects();
        }

        protected override void OnLateUpdate()
        {
            Apply();
        }

        public void Apply()
        {
            var panOffset = pan != null ? pan.Offset : Vector2.zero;
            var shakeOffset = shake != null ? shake.Offset : Vector2.zero;
            var shakeRoll = shake != null ? shake.Roll : 0f;
            var factor = zoom != null ? zoom.Factor : 1f;
            var minFactor = zoom != null ? zoom.MinFactor : 1f;
            var maxFactor = zoom != null ? zoom.MaxFactor : 1f;

            if (target != null)
            {
                target.position = CameraComposition.Compose(basePosition, panOffset, shakeOffset);

                var roll = CameraComposition.ComposeRoll(baseRoll, shakeRoll);

                if (roll != 0f || rollApplied)
                {
                    rollApplied = roll != 0f;
                    target.rotation = Quaternion.Euler(0f, 0f, roll);
                }
            }

            if (camera != null)
                camera.orthographicSize = CameraComposition.ComposeSize(baseSize, factor, minFactor, maxFactor);
        }
    }
}
