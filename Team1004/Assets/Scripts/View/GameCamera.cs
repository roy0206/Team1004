using UnityEngine;

namespace Game.View
{
    [RequireComponent(typeof(Camera))]
    public sealed class GameCamera : MonoThing
    {
        [SerializeField] private float aspectWidth = 16f;
        [SerializeField] private float aspectHeight = 9f;
        [SerializeField] private CameraFxSettings fxSettings;
        [SerializeField] private float shakeSeed = 12.7f;

        private AspectModule aspect;
        private CameraShakeModule shake;
        private CameraZoomModule zoom;
        private CameraPanModule pan;
        private CameraRig rig;

        public AspectModule Aspect => aspect;
        public CameraRig Rig => rig;
        public CameraFxSettings FxSettings => fxSettings;

        private void Awake()
        {
            var view = GetComponent<Camera>();

            aspect = AddModule(new AspectModule(view, aspectWidth / Mathf.Max(0.01f, aspectHeight)));
            shake = AddModule(new CameraShakeModule(shakeSeed));
            zoom = AddModule(new CameraZoomModule());
            pan = AddModule(new CameraPanModule());
            rig = AddModule(new CameraRig(view, transform, shake, zoom, pan));
            rig.ApplySettings(fxSettings);
        }

        private void OnEnable()
        {
            rig?.ResetEffects();
        }
    }
}
