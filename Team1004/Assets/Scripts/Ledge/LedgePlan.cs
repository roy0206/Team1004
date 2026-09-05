using UnityEngine;

namespace Game.Ledge
{
    public readonly struct LedgePlan
    {
        public bool IsValid { get; }
        public float LedgeTime { get; }
        public float ApproachTime { get; }
        public float StartTime { get; }
        public float StartX { get; }
        public float PlayerX { get; }
        public float ScrollSpeed { get; }

        private LedgePlan(
            float ledgeTime,
            float approachTime,
            float startTime,
            float startX,
            float playerX,
            float scrollSpeed)
        {
            IsValid = true;
            LedgeTime = ledgeTime;
            ApproachTime = approachTime;
            StartTime = startTime;
            StartX = startX;
            PlayerX = playerX;
            ScrollSpeed = scrollSpeed;
        }

        public static LedgePlan None => default;

        public static LedgePlan Create(
            float ledgeTime,
            float approachSafeTime,
            float scrollSpeed,
            float playerX,
            float spawnX)
        {
            if (ledgeTime <= 0f || scrollSpeed <= 0f || spawnX <= playerX)
                return default;

            var safeTime = Mathf.Max(0f, approachSafeTime);
            var travelTime = (spawnX - playerX) / scrollSpeed;
            var lead = Mathf.Max(safeTime, travelTime);
            var startTime = Mathf.Max(0f, ledgeTime - lead);
            var startX = playerX + scrollSpeed * (ledgeTime - startTime);
            var approachTime = Mathf.Max(startTime, ledgeTime - safeTime);

            return new LedgePlan(ledgeTime, approachTime, startTime, startX, playerX, scrollSpeed);
        }

        public float FrontXAt(float time)
        {
            if (!IsValid)
                return float.PositiveInfinity;

            return StartX - ScrollSpeed * (time - StartTime);
        }

        public float TimeAtFrontX(float x)
        {
            if (!IsValid || ScrollSpeed <= 0f)
                return 0f;

            return StartTime + (StartX - x) / ScrollSpeed;
        }
    }
}
