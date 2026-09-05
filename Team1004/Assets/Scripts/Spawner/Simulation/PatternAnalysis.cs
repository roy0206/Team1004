namespace Game.Spawner
{
    public sealed class PatternAnalysis
    {
        public PatternAnalysis(
            PatternSpec pattern,
            float speedMultiplier,
            string error,
            bool solvableWithoutJump,
            bool solvableWithJump,
            int worstCaseInputs,
            float reactionMargin,
            float earliestArrival,
            float blockDuration)
        {
            Pattern = pattern;
            SpeedMultiplier = speedMultiplier;
            Error = error;
            SolvableWithoutJump = solvableWithoutJump;
            SolvableWithJump = solvableWithJump;
            WorstCaseInputs = worstCaseInputs;
            ReactionMargin = reactionMargin;
            EarliestArrival = earliestArrival;
            BlockDuration = blockDuration;
        }

        public PatternSpec Pattern { get; }
        public float SpeedMultiplier { get; }
        public string Error { get; }
        public bool SolvableWithoutJump { get; }
        public bool SolvableWithJump { get; }
        public int WorstCaseInputs { get; }
        public float ReactionMargin { get; }
        public float EarliestArrival { get; }
        public float BlockDuration { get; }

        public bool IsValid => Error == null;
        public bool Solvable => IsValid && (SolvableWithoutJump || SolvableWithJump);
        public bool JumpRequired => IsValid && !SolvableWithoutJump && SolvableWithJump;
        public bool ManualMismatch => Solvable && Pattern.ManualJumpRequired != JumpRequired;

        public int Tier
        {
            get
            {
                if (!Solvable)
                    return 3;

                var tier = WorstCaseInputs <= 1 ? 1 : WorstCaseInputs == 2 ? 2 : 3;

                if (ReactionMargin >= 0f && ReactionMargin < ShortMarginThreshold && tier < 3)
                    tier++;

                return tier;
            }
        }

        public const float ShortMarginThreshold = 1.5f;

        public static PatternAnalysis Invalid(PatternSpec pattern, float speedMultiplier, string error)
        {
            return new PatternAnalysis(pattern, speedMultiplier, error ?? "invalid", false, false, 0, -1f, 0f, 0f);
        }
    }
}
