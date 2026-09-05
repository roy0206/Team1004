using UnityEngine;

namespace Game.Play
{
    public sealed class ScrollModule : Module
    {
        private readonly Transform root;

        public float Speed { get; set; }
        public bool IsScrolling { get; set; }
        public float Scrolled { get; private set; }

        public ScrollModule(Transform root, float speed)
        {
            this.root = root;
            Speed = speed;
        }

        protected override ModuleTick Ticks => ModuleTick.Update;

        protected override void OnUpdate()
        {
            if (!IsScrolling || root == null)
                return;

            var dx = Speed * Time.deltaTime;
            if (dx <= 0f)
                return;

            Scrolled += dx;
            var shift = new Vector3(-dx, 0f, 0f);

            for (var i = 0; i < root.childCount; i++)
                root.GetChild(i).localPosition += shift;
        }
    }
}
