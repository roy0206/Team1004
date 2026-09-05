namespace Game.Spawner
{
    public sealed class ObstacleTiming
    {
        public ObstacleTiming(
            ObstacleSpec spec,
            int startLane,
            float spawnTime,
            float spawnCenterX,
            float speed,
            float appearTime,
            float blockStart,
            float blockEnd)
        {
            Spec = spec;
            StartLane = startLane;
            LaneMask = Game.Spawner.LaneMask.Span(startLane, spec.LaneSpan);
            SpawnTime = spawnTime;
            SpawnCenterX = spawnCenterX;
            Speed = speed;
            AppearTime = appearTime;
            BlockStart = blockStart;
            BlockEnd = blockEnd;
        }

        public ObstacleSpec Spec { get; }
        public int StartLane { get; }
        public int LaneMask { get; }
        public float SpawnTime { get; }
        public float SpawnCenterX { get; }
        public float Speed { get; }
        public float AppearTime { get; }
        public float BlockStart { get; }
        public float BlockEnd { get; }

        public float CenterXAt(float time)
        {
            return SpawnCenterX - Speed * (time - SpawnTime);
        }
    }
}
