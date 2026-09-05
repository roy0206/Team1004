using System;
using Game.StateMachine;

namespace Game.Boss
{
    public abstract class TelegraphedAttackPhase<TContext, TKey> : TimedState<TContext, TKey>
    {
        private readonly BossTimer timer;
        private BossAttackStep step;
        private float stepElapsed;
        private float currentTelegraph;
        private bool imminentRaised;
        private int attackCount;

        protected TelegraphedAttackPhase(
            StateMachine<TContext, TKey> machine,
            BossTimer timer,
            BossAttackTiming timing,
            TKey next)
            : base(machine, timer != null ? timer.Duration : 0f, next)
        {
            this.timer = timer ?? throw new ArgumentNullException(nameof(timer));
            Timing = timing;
        }

        public BossTimer Timer => timer;
        public BossAttackTiming Timing { get; set; }
        public float ImminentLead { get; set; } = 0.2f;
        public BossAttackStep Step => step;
        public float StepElapsed => stepElapsed;
        public float CurrentTelegraph => currentTelegraph;
        public int AttackCount => attackCount;
        public bool IsAttacking => step == BossAttackStep.Attack;

        public float StepDuration
        {
            get
            {
                switch (step)
                {
                    case BossAttackStep.Telegraph:
                        return currentTelegraph;
                    case BossAttackStep.Attack:
                        return Timing.Attack;
                    case BossAttackStep.Recovery:
                        return Timing.Recovery;
                    default:
                        return 0f;
                }
            }
        }

        public float Step01
        {
            get
            {
                var duration = StepDuration;

                if (duration <= 0f)
                    return 1f;

                var value = stepElapsed / duration;
                return value < 0f ? 0f : value > 1f ? 1f : value;
            }
        }

        protected sealed override void OnTimedEnter(TContext context)
        {
            Duration = timer.Duration;
            step = BossAttackStep.Idle;
            stepElapsed = 0f;
            currentTelegraph = Timing.Telegraph;
            imminentRaised = false;
            attackCount = 0;
            timer.Start();
            OnPhaseEnter(context);
        }

        protected sealed override void OnTimedUpdate(TContext context, float deltaTime)
        {
            timer.Tick(deltaTime);
            stepElapsed += deltaTime;

            switch (step)
            {
                case BossAttackStep.Idle:
                    TryStartCycle(context);
                    break;

                case BossAttackStep.Telegraph:
                    OnStepUpdate(context, deltaTime);

                    if (step != BossAttackStep.Telegraph)
                        break;

                    if (!imminentRaised && ImminentLead > 0f && stepElapsed >= currentTelegraph - ImminentLead)
                    {
                        imminentRaised = true;
                        OnTelegraphImminent(context);
                    }

                    if (step == BossAttackStep.Telegraph && stepElapsed >= currentTelegraph)
                    {
                        EnterStep(BossAttackStep.Attack);
                        OnAttackBegin(context);
                    }

                    break;

                case BossAttackStep.Attack:
                    OnStepUpdate(context, deltaTime);

                    if (step == BossAttackStep.Attack && stepElapsed >= Timing.Attack)
                    {
                        EnterStep(BossAttackStep.Recovery);
                        OnAttackEnd(context);
                    }

                    break;

                case BossAttackStep.Recovery:
                    OnStepUpdate(context, deltaTime);

                    if (step == BossAttackStep.Recovery && stepElapsed >= Timing.Recovery)
                    {
                        EnterStep(BossAttackStep.Idle);
                        OnRecoveryEnd(context);
                        TryStartCycle(context);
                    }

                    break;
            }
        }

        protected sealed override void OnTimeout(TContext context)
        {
        }

        public sealed override void OnExit(TContext context)
        {
            timer.Stop();
            OnPhaseExit(context);
            step = BossAttackStep.Idle;
            stepElapsed = 0f;
        }

        protected virtual void OnPhaseEnter(TContext context)
        {
        }

        protected virtual bool CanStartAttack(TContext context)
        {
            return true;
        }

        protected abstract void OnTelegraphBegin(TContext context);

        protected virtual float GetTelegraphDuration(TContext context)
        {
            return Timing.Telegraph;
        }

        protected virtual void OnTelegraphImminent(TContext context)
        {
        }

        protected abstract void OnAttackBegin(TContext context);

        protected abstract void OnAttackEnd(TContext context);

        protected virtual void OnRecoveryEnd(TContext context)
        {
        }

        protected virtual void OnStepUpdate(TContext context, float deltaTime)
        {
        }

        protected virtual void OnPhaseExpired(TContext context)
        {
        }

        protected virtual void OnPhaseExit(TContext context)
        {
        }

        private void TryStartCycle(TContext context)
        {
            if (timer.IsExpired)
            {
                OnPhaseExpired(context);
                Finish();
                return;
            }

            if (!CanStartAttack(context))
                return;

            attackCount++;
            EnterStep(BossAttackStep.Telegraph);
            imminentRaised = false;
            OnTelegraphBegin(context);
            currentTelegraph = GetTelegraphDuration(context);

            if (currentTelegraph < 0f)
                currentTelegraph = 0f;
        }

        private void EnterStep(BossAttackStep next)
        {
            step = next;
            stepElapsed = 0f;
        }
    }
}
