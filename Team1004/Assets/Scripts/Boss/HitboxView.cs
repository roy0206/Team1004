using UnityEngine;

namespace Game.Boss
{
    public sealed class HitboxView : MonoThing
    {
        [SerializeField] private SpriteRenderer frame;
        [SerializeField] private BoxCollider2D box;
        [SerializeField] private Color color = new(1f, 0.15f, 0.15f, 1f);

        private HitboxViewModule module;

        public bool IsVisible => module != null && module.IsVisible;
        public SpriteRenderer Frame => frame;
        public BoxCollider2D Box => box;

        private void Awake()
        {
            EnsureModule();
        }

        public void SetVisible(bool visible)
        {
            EnsureModule().SetVisible(visible);
        }

        protected override void OnThingDestroy()
        {
            module = null;
        }

        private HitboxViewModule EnsureModule()
        {
            if (module == null || !module.IsAttached)
                module = AddModule(new HitboxViewModule(frame, box, color));

            return module;
        }
    }
}
