using UnityEngine;

namespace Game.View
{
    [RequireComponent(typeof(Camera))]
    public sealed class GameCamera : MonoThing
    {
        [SerializeField] private float aspectWidth = 16f;
        [SerializeField] private float aspectHeight = 9f;

        private AspectModule aspect;

        public AspectModule Aspect => aspect;

        private void Awake()
        {
            aspect = AddModule(new AspectModule(GetComponent<Camera>(), aspectWidth / Mathf.Max(0.01f, aspectHeight)));
        }
    }
}
