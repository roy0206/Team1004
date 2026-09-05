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
            float collisionHeight,
            float speedMultiplier,
            float spawnWeight,
            bool usableInNormalPatterns,
            bool usableInJumpPatterns,
            ObstacleFitAxis fitAxis = ObstacleFitAxis.Length,
            float visualHeight = 0f)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Obstacle id is empty.", nameof(id));

            Id = id;
            PoolKey = string.IsNullOrWhiteSpace(poolKey) ? id : poolKey;
            LaneSpan = Math.Max(1, laneSpan);
            AllowedStartLaneMask = allowedStartLaneMask;
            BodyLength = Math.Max(0.01f, bodyLength);
            CollisionLength = collisionLength > 0f ? collisionLength : BodyLength;
            CollisionHeight = Math.Max(0.01f, collisionHeight);
            SpeedMultiplier = Math.Max(0.01f, speedMultiplier);
            SpawnWeight = Math.Max(0f, spawnWeight);
            UsableInNormalPatterns = usableInNormalPatterns;
            UsableInJumpPatterns = usableInJumpPatterns;
            FitAxis = fitAxis;
            VisualHeight = Math.Max(0f, visualHeight);

            if (FitAxis == ObstacleFitAxis.Height && VisualHeight <= 0f)
                FitAxis = ObstacleFitAxis.Length;
        }

        public string Id { get; }
        public string PoolKey { get; }
        public int LaneSpan { get; }
        public int AllowedStartLaneMask { get; }
        public float BodyLength { get; }
        public float CollisionLength { get; }
        public float CollisionHeight { get; }
        public float SpeedMultiplier { get; }
        public float SpawnWeight { get; }
        public bool UsableInNormalPatterns { get; }
        public bool UsableInJumpPatterns { get; }
        public ObstacleFitAxis FitAxis { get; }
        public float VisualHeight { get; }

        public float HalfBody => BodyLength * 0.5f;
        public float HalfCollision => CollisionLength * 0.5f;

        public bool CanStartAt(int lane, int laneCount)
        {
            return lane >= 0 && lane + LaneSpan <= laneCount && LaneMask.Contains(AllowedStartLaneMask, lane);
        }

        public bool ReachesNeighbourLane(float laneSpacing, float playerCollisionHeight)
        {
            var reach = (CollisionHeight + Math.Max(0f, playerCollisionHeight)) * 0.5f;
            var freeLaneDistance = laneSpacing * (LaneSpan + 1) * 0.5f;
            return reach >= freeLaneDistance - 0.0001f;
        }
    }
}
