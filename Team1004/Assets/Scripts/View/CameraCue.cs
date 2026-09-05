using UnityEngine;

namespace Game.View
{
    [CreateAssetMenu(menuName = "Team1004/Camera Cue", fileName = "CameraCue")]
    public sealed class CameraCue : ScriptableObject
    {
        [SerializeField] private string cueId;
        [SerializeField] private float trauma;
        [SerializeField] private float shakeDuration;
        [SerializeField] private Vector2 punchDirection = Vector2.up;
        [SerializeField] private float punchStrength;
        [SerializeField] private float punchDuration = 0.2f;
        [SerializeField] private float zoomFactor;
        [SerializeField] private float zoomInDuration = 0.2f;
        [SerializeField] private float zoomHold;
        [SerializeField] private float zoomOutDuration;
        [SerializeField] private CameraEase zoomEase = CameraEase.SineInOut;
        [SerializeField] private Vector2 panOffset;
        [SerializeField] private float panInDuration = 0.3f;
        [SerializeField] private float panHold;
        [SerializeField] private float panOutDuration;
        [SerializeField] private CameraEase panEase = CameraEase.SineInOut;

        public string CueId => string.IsNullOrEmpty(cueId) ? name : cueId;
        public float Trauma => trauma;
        public float ShakeDuration => shakeDuration;
        public Vector2 PunchDirection => punchDirection;
        public float PunchStrength => punchStrength;
        public float PunchDuration => punchDuration;
        public float ZoomFactor => zoomFactor;
        public float ZoomInDuration => zoomInDuration;
        public float ZoomHold => zoomHold;
        public float ZoomOutDuration => zoomOutDuration;
        public CameraEase ZoomEase => zoomEase;
        public Vector2 PanOffset => panOffset;
        public float PanInDuration => panInDuration;
        public float PanHold => panHold;
        public float PanOutDuration => panOutDuration;
        public CameraEase PanEase => panEase;

        public bool HasShake => trauma > 0f;
        public bool HasPunch => punchStrength > 0f;
        public bool HasZoom => zoomFactor > 0f;
        public bool HasPan => panOffset.sqrMagnitude > 0.000001f;

        public void Apply(CameraRig rig)
        {
            Apply(rig, punchDirection);
        }

        public void Apply(CameraRig rig, Vector2 direction)
        {
            if (rig == null)
                return;

            var shake = rig.Shake;

            if (shake != null)
            {
                if (trauma > 0f)
                {
                    if (shakeDuration > 0f)
                        shake.ShakeFor(shakeDuration, trauma);
                    else
                        shake.AddTrauma(trauma);
                }

                if (punchStrength > 0f)
                    shake.Punch(direction, punchStrength, punchDuration);
            }

            var zoom = rig.Zoom;

            if (zoom != null && zoomFactor > 0f)
            {
                if (zoomOutDuration > 0f)
                    zoom.ZoomPunch(zoomFactor, zoomInDuration, zoomHold, zoomOutDuration, zoomEase);
                else
                    zoom.ZoomTo(zoomFactor, zoomInDuration, zoomEase);
            }

            var pan = rig.Pan;

            if (pan == null || !HasPan)
                return;

            if (panOutDuration > 0f)
                pan.PanPulse(panOffset, panInDuration, panHold, panOutDuration, panEase);
            else
                pan.PanTo(panOffset, panInDuration, panEase);
        }
    }
}
