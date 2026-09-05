using System;

namespace Game.Boss
{
    public sealed class BossLifecycle
    {
        public BossStage Stage { get; private set; } = BossStage.Idle;
        public BossOutcome Outcome { get; private set; } = BossOutcome.None;

        public bool IsInitialized => Stage != BossStage.Idle;
        public bool IsActive => Stage == BossStage.Active;
        public bool IsFinished => Stage == BossStage.Finished;

        public event Action Began;
        public event Action<BossOutcome> Finished;

        public bool Initialize()
        {
            if (Stage == BossStage.Active)
                return false;

            Stage = BossStage.Ready;
            Outcome = BossOutcome.None;
            return true;
        }

        public bool Begin()
        {
            if (Stage == BossStage.Idle)
                throw new InvalidOperationException("Initialize must be called before Begin.");

            if (Stage != BossStage.Ready)
                return false;

            Stage = BossStage.Active;
            Began?.Invoke();
            return true;
        }

        public bool Complete(BossOutcome outcome)
        {
            if (outcome == BossOutcome.None)
                throw new ArgumentException("Outcome must be Passed or Failed.", nameof(outcome));

            if (Stage != BossStage.Active)
                return false;

            Finish(outcome);
            return true;
        }

        public bool Abort()
        {
            if (Stage != BossStage.Ready && Stage != BossStage.Active)
                return false;

            Finish(BossOutcome.Failed);
            return true;
        }

        public void Reset()
        {
            Stage = BossStage.Idle;
            Outcome = BossOutcome.None;
        }

        private void Finish(BossOutcome outcome)
        {
            Stage = BossStage.Finished;
            Outcome = outcome;
            Finished?.Invoke(outcome);
        }
    }
}
