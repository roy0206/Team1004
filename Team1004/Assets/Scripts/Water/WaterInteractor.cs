using UnityEngine;

namespace Game.Water
{
    public sealed class WaterInteractor : MonoThing
    {
        [SerializeField] private Collider2D shape;
        [SerializeField] private Vector2 size = Vector2.one;
        [SerializeField] private Vector2 offset;

        private WaterInteractorModule module;

        public WaterInteractorModule Module => module;
        public bool IsTouchingWater => module != null && module.IsTouchingWater;

        private void Awake()
        {
            if (shape == null)
                shape = GetComponent<Collider2D>();

            var smoothing = WaterSettings.currentSettings != null ? WaterSettings.currentSettings.velocitySmoothing : 0f;
            module = shape != null
                ? AddModule(new WaterInteractorModule(transform, shape, smoothing))
                : AddModule(new WaterInteractorModule(transform, size, offset, smoothing));
        }

        protected override void OnThingDestroy()
        {
            module = null;
        }
    }
}
