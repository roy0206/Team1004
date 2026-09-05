using System;

namespace Game.Spawner
{
    public sealed class ObstacleSpec
    {
        public ObstacleSpec(
            string id,
            string poolKey,
            int laneSpan,
            int allowedStartLaneMask,
            float bodyLength,
            float collisionLength,
            float speedMultiplier,
            bool usableInNormalPatterns,
            bool usableInJumpPatterns)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Obstacle id is empty.", nameof(id));

            Id = id;
            PoolKey = string.IsNullOrWhiteSpace(poolKey) ? id : poolKey;
            LaneSpan = Math.Max(1, laneSpan);
            AllowedStartLaneMask = allowedStartLaneMask;
            BodyLength = Math.Max(0.01f, bodyLength);
            CollisionLength = collisionLength > 0f ? collisionLength : BodyLength;
            SpeedMultiplier = Math.Max(0.01f, speedMultiplier);
            UsableInNormalPatterns = usableInNormalPatterns;
            UsableInJumpPatterns = usableInJumpPatterns;
        }

        public string Id { get; }
        public string PoolKey { get; }
        public int LaneSpan { get; }
        public int AllowedStartLaneMask { get; }
        public float BodyLength { get; }
        public float CollisionLength { get; }
        public float SpeedMultiplier { get; }
        public bool UsableInNormalPatterns { get; }
        public bool UsableInJumpPatterns { get; }

        public float HalfBody => BodyLength * 0.5f;
        public float HalfCollision => CollisionLength * 0.5f;

        public bool CanStartAt(int lane, int laneCount)
        {
            return lane >= 0 && lane + LaneSpan <= laneCount && LaneMask.Contains(AllowedStartLaneMask, lane);
        }
    }
}
