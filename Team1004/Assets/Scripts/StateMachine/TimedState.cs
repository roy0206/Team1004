using System;

namespace Game.StateMachine
{
    public class TimedState<TContext, TKey> : State<TContext>
    {
        private readonly StateMachine<TContext, TKey> machine;
        private float elapsed;

        public TimedState(StateMachine<TContext, TKey> machine, float duration, TKey next)
        {
            this.machine = machine ?? throw new ArgumentNullException(nameof(machine));
            Duration = duration;
            Next = next;
        }

        public float Duration { get; set; }
        public TKey Next { get; set; }
        public float Elapsed => elapsed;
        public float Remaining => Duration > elapsed ? Duration - elapsed : 0f;

        protected StateMachine<TContext, TKey> Machine => machine;

        public sealed override void OnEnter(TContext context)
        {
            elapsed = 0f;
            OnTimedEnter(context);
        }

        public sealed override void OnUpdate(TContext context, float deltaTime)
        {
            elapsed += deltaTime;
            OnTimedUpdate(context, deltaTime);

            if (!ReferenceEquals(machine.Current, this))
                return;

            if (elapsed >= Duration)
                OnTimeout(context);
        }

        protected virtual void OnTimedEnter(TContext context)
        {
        }

        protected virtual void OnTimedUpdate(TContext context, float deltaTime)
        {
        }

        protected virtual void OnTimeout(TContext context)
        {
            Finish();
        }

        protected void Finish()
        {
            machine.ChangeState(Next);
        }
    }
}
