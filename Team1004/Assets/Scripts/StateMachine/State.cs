namespace Game.StateMachine
{
    public abstract class State<TContext> : IState<TContext>
    {
        public virtual void OnEnter(TContext context)
        {
        }

        public virtual void OnUpdate(TContext context, float deltaTime)
        {
        }

        public virtual void OnFixedUpdate(TContext context, float fixedDeltaTime)
        {
        }

        public virtual void OnExit(TContext context)
        {
        }
    }
}
