using System;

namespace Game.Spawner
{
    public sealed class SimulationConfig
    {
        public int LaneCount { get; set; } = 3;
        public float PlayerX { get; set; } = -4.2f;
        public float PlayerHalfWidth { get; set; } = SpawnerDefaults.PlayerHalfWidth;
        public float ScreenRightX { get; set; } = 6.4f;
        public float ScreenLeftX { get; set; } = -6.4f;
        public float ScrollSpeed { get; set; } = 4f;
        public float LaneMoveDuration { get; set; } = 0.2f;
        public float JumpDuration { get; set; } = SpawnerDefaults.JumpDurationSeconds;
        public float JumpCooldown { get; set; } = 1.2f;
        public float TickSeconds { get; set; } = 0.05f;
        public float SafetyPadding { get; set; } = 0.1f;
        public float MinJumpPatternGap { get; set; } = 2f;
        public float SectionStartGrace { get; set; } = 2f;
        public int MaxCandidateAttempts { get; set; } = 4;
        public float RetryInterval { get; set; } = 0.2f;

        public int TopLane => 0;
        public int BottomLane => LaneCount - 1;

        public int TimeToTick(float seconds)
        {
            return (int)Math.Round(seconds / TickSeconds);
        }

        public int DurationToTicks(float seconds)
        {
            return Math.Max(0, (int)Math.Ceiling(seconds / TickSeconds - 0.0001f));
        }

        public float ApproachSpeed(ObstacleSpec obstacle, float speedMultiplier)
        {
            return ScrollSpeed * obstacle.SpeedMultiplier * speedMultiplier;
        }

        public float EarliestArrivalDelay(ObstacleSpec obstacle, float speedMultiplier)
        {
            var distance = ScreenRightX + obstacle.HalfBody - obstacle.HalfCollision - PlayerX - PlayerHalfWidth;
            return Math.Max(0f, distance) / ApproachSpeed(obstacle, speedMultiplier);
        }

        public bool Validate(out string error)
        {
            if (LaneCount < 1)
            {
                error = "LaneCount must be 1 or greater.";
                return false;
            }

            if (ScrollSpeed <= 0f)
            {
                error = "ScrollSpeed must be greater than 0.";
                return false;
            }

            if (TickSeconds <= 0f)
            {
                error = "TickSeconds must be greater than 0.";
                return false;
            }

            if (ScreenRightX <= PlayerX)
            {
                error = "ScreenRightX must be greater than PlayerX.";
                return false;
            }

            if (LaneMoveDuration < 0f || JumpDuration < 0f || JumpCooldown < 0f || SafetyPadding < 0f || SectionStartGrace < 0f)
            {
                error = "Durations must be 0 or greater.";
                return false;
            }

            if (MaxCandidateAttempts < 1)
            {
                error = "MaxCandidateAttempts must be 1 or greater.";
                return false;
            }

            if (RetryInterval <= 0f)
            {
                error = "RetryInterval must be greater than 0.";
                return false;
            }

            error = null;
            return true;
        }

        public SimulationConfig Clone()
        {
            return (SimulationConfig)MemberwiseClone();
        }
    }
}
