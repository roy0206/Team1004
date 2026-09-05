using UnityEngine;

namespace Game.Water
{
    public sealed class WaterInteractor : MonoThing, IPooledObject
    {
        [SerializeField] private Collider2D shape;
        [SerializeField] private Vector2 size = Vector2.one;
        [SerializeField] private Vector2 offset;
        [SerializeField] private bool ignoreColliderShape;
        [SerializeField] private float surfaceInfluence;
        [SerializeField] private bool wakeOnly;
        [SerializeField] private float wakeScale = 1f;

        private WaterInteractorModule module;

        public WaterInteractorModule Module => module;
        public bool IsTouchingWater => module != null && module.IsTouchingWater;

        public bool WakeOnly
        {
            get => module != null ? module.WakeOnly : wakeOnly;
            set
            {
                wakeOnly = value;

                if (module != null)
                    module.WakeOnly = value;
            }
        }

        public float WakeScale
        {
            get => module != null ? module.WakeScale : wakeScale;
            set
            {
                wakeScale = Mathf.Max(0f, value);

                if (module != null)
                    module.WakeScale = wakeScale;
            }
        }

        public float SurfaceInfluence
        {
            get => module != null ? module.SurfaceInfluence : surfaceInfluence;
            set
            {
                surfaceInfluence = value;

                if (module != null)
                    module.SurfaceInfluence = value;
            }
        }

        public bool IsWarmingUp => module != null && module.IsWarmingUp;

        public void ResetVelocity()
        {
            if (module != null)
                module.ResetVelocity();
        }

        public void NotifyTeleport()
        {
            if (module != null)
                module.NotifyTeleport();
        }

        public static void NotifyTeleport(Transform target)
        {
            if (target == null)
                return;

            var interactors = target.GetComponentsInChildren<WaterInteractor>(true);

            for (var i = 0; i < interactors.Length; i++)
                interactors[i].NotifyTeleport();
        }

        public void OnSpawned()
        {
            ResetVelocity();
        }

        public void OnReleased()
        {
            ResetVelocity();
        }

        private void Awake()
        {
            if (ignoreColliderShape)
                shape = null;
            else if (shape == null)
                shape = GetComponent<Collider2D>();

            var smoothing = WaterSettings.currentSettings != null ? WaterSettings.currentSettings.velocitySmoothing : 0f;
            module = shape != null
                ? AddModule(new WaterInteractorModule(transform, shape, smoothing, surfaceInfluence, wakeOnly, wakeScale))
                : AddModule(new WaterInteractorModule(transform, size, offset, smoothing, surfaceInfluence, wakeOnly, wakeScale));
        }

        private void OnEnable()
        {
            ResetVelocity();
        }

        protected override void OnThingDestroy()
        {
            module = null;
        }
    }
}
