using Game.Config;
using UnityEngine;

namespace Game.Spawner
{
    [CreateAssetMenu(fileName = "ObstacleSpawnerSettings", menuName = "Team1004/Spawner/Spawner Settings")]
    public sealed class ObstacleSpawnerSettings : ScriptableObject
    {
        [SerializeField] private PatternLibrary library;
        [SerializeField] private DifficultyCurve difficulty;
        [SerializeField] private float screenRightX = 6.4f;
        [SerializeField] private float screenLeftX = -6.4f;
        [SerializeField, Min(0f)] private float playerHalfWidth = SpawnerDefaults.PlayerHalfWidth;
        [SerializeField, Min(0.01f)] private float tickSeconds = 0.05f;
        [SerializeField, Min(0f)] private float safetyPadding = 0.1f;
        [SerializeField, Min(0f)] private float minJumpPatternGap = 2f;
        [SerializeField, Min(0f)] private float sectionStartGrace = 2f;
        [SerializeField, Min(0.01f)] private float retryInterval = 0.2f;
        [SerializeField, Min(1)] private int maxCandidateAttempts = 4;
        [SerializeField, Min(0f)] private float despawnMargin = 1f;
        [SerializeField, Min(0)] private int prewarmPerObstacle = 4;
        [SerializeField] private int defaultSeed = SpawnerDefaults.DefaultSeed;

        public PatternLibrary Library => library;
        public DifficultyCurve Difficulty => difficulty;
        public float ScreenRightX => screenRightX;
        public float ScreenLeftX => screenLeftX;
        public float PlayerHalfWidth => playerHalfWidth;
        public float TickSeconds => tickSeconds;
        public float SafetyPadding => safetyPadding;
        public float MinJumpPatternGap => minJumpPatternGap;
        public float SectionStartGrace => sectionStartGrace;
        public float RetryInterval => retryInterval;
        public int MaxCandidateAttempts => maxCandidateAttempts;
        public float DespawnMargin => despawnMargin;
        public int PrewarmPerObstacle => prewarmPerObstacle;
        public int DefaultSeed => defaultSeed;

        public SimulationConfig CreateSimulationConfig(GameConfigValues values)
        {
            values ??= new GameConfigValues();

            return new SimulationConfig
            {
                LaneCount = Mathf.Max(1, values.LaneCount),
                PlayerX = values.PlayerX,
                PlayerHalfWidth = playerHalfWidth,
                ScreenRightX = screenRightX,
                ScreenLeftX = screenLeftX,
                ScrollSpeed = values.ScrollSpeed,
                LaneMoveDuration = values.LaneMoveDuration,
                JumpDuration = values.JumpDuration,
                JumpCooldown = values.JumpCooldown,
                TickSeconds = tickSeconds,
                SafetyPadding = safetyPadding,
                MinJumpPatternGap = minJumpPatternGap,
                SectionStartGrace = sectionStartGrace,
                MaxCandidateAttempts = maxCandidateAttempts,
                RetryInterval = retryInterval
            };
        }

#if UNITY_EDITOR
        public void EditorInitialize(PatternLibrary library, DifficultyCurve difficulty)
        {
            this.library = library;
            this.difficulty = difficulty;
        }
#endif
    }
}
