using UnityEngine;

namespace Game.Ledge
{
    public sealed class LedgeScrollModule : Module
    {
        private readonly Transform target;
        private readonly float frontOffsetX;
        private readonly float baseY;
        private readonly float parkX;

        public float FrontX { get; private set; }
        public float VerticalOffset { get; private set; }

        public LedgeScrollModule(Transform target, float frontOffsetX, float parkX)
        {
            this.target = target;
            this.frontOffsetX = frontOffsetX;
            this.parkX = parkX;
            baseY = target != null ? target.localPosition.y : 0f;
            FrontX = parkX;
        }

        protected override ModuleTick Ticks => ModuleTick.None;

        public void Place(float frontX, float verticalOffset)
        {
            FrontX = frontX;
            VerticalOffset = verticalOffset;

            if (target == null)
                return;

            var position = target.localPosition;
            position.x = frontX - frontOffsetX;
            position.y = baseY + verticalOffset;
            target.localPosition = position;
        }

        public void Park()
        {
            Place(parkX, 0f);
        }
    }
}
