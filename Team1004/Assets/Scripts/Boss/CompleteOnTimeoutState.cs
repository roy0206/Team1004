using Game.StateMachine;

namespace Game.Boss
{
    public class CompleteOnTimeoutState<TBoss, TKey> : TimedState<TBoss, TKey> where TBoss : BossThing
    {
        private readonly BossOutcome outcome;

        public CompleteOnTimeoutState(StateMachine<TBoss, TKey> machine, float duration, BossOutcome outcome)
            : base(machine, duration, default)
        {
            this.outcome = outcome;
        }

        public BossOutcome Outcome => outcome;

        protected override void OnTimeout(TBoss boss)
        {
            boss.Complete(outcome);
        }
    }
}
