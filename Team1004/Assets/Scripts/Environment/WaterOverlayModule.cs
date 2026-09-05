using UnityEngine.U2D;

namespace Game.Environment
{
    public sealed class WaterOverlayModule : Module
    {
        private readonly SpriteShapeController source;
        private readonly SpriteShapeController target;

        public SpriteShapeController Source => source;
        public SpriteShapeController Target => target;
        public bool IsUsable => source != null && target != null;

        public WaterOverlayModule(SpriteShapeController source, SpriteShapeController target)
        {
            this.source = source;
            this.target = target;
        }

        protected override ModuleTick Ticks => ModuleTick.LateUpdate;

        protected override void OnLateUpdate()
        {
            Sync();
        }

        public bool Sync()
        {
            if (!IsUsable)
                return false;

            var from = source.spline;
            var to = target.spline;
            var count = from.GetPointCount();

            if (count < 3)
                return false;

            to.Clear();
            to.isOpenEnded = from.isOpenEnded;

            for (var i = 0; i < count; i++)
            {
                to.InsertPointAt(i, from.GetPosition(i));
                to.SetHeight(i, from.GetHeight(i));
                to.SetSpriteIndex(i, from.GetSpriteIndex(i));
                to.SetTangentMode(i, from.GetTangentMode(i));
                to.SetLeftTangent(i, from.GetLeftTangent(i));
                to.SetRightTangent(i, from.GetRightTangent(i));
            }

            target.BakeMesh();
            return true;
        }
    }
}
