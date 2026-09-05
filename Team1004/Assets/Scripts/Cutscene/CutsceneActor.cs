using Game.Animation;
using UnityEngine;

namespace Game.Cutscene
{
    public sealed class CutsceneActor : MonoThing
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private CutsceneActorViewModule view;
        private SpriteAnimatorModule animator;

        public string Id => id;
        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public CutsceneActorViewModule View => EnsureView();
        public bool IsVisible => EnsureView().IsVisible;
        public SpriteAnimatorModule Animator => EnsureAnimator();
        public bool HasAnimator => EnsureAnimator() != null;

        public float Alpha
        {
            get => EnsureView().Alpha;
            set => EnsureView().Alpha = value;
        }

        private void Awake()
        {
            EnsureView();
        }

        public void SetVisible(bool visible)
        {
            EnsureView().SetVisible(visible);
        }

        public void SetSprite(Sprite sprite)
        {
            EnsureView().SetSprite(sprite);
        }

        private SpriteAnimatorModule EnsureAnimator()
        {
            if (animator != null && animator.IsAttached)
                return animator;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
                return null;

            animator = AddModule(new SpriteAnimatorModule(spriteRenderer));
            return animator;
        }

        private CutsceneActorViewModule EnsureView()
        {
            if (view != null && view.IsAttached)
                return view;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            view = AddModule(new CutsceneActorViewModule(gameObject, spriteRenderer));
            return view;
        }
    }
}
