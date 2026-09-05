namespace Game.StateMachine
{
    public interface IState<TContext>
    {
        void OnEnter(TContext context);
        void OnUpdate(TContext context, float deltaTime);
        void OnFixedUpdate(TContext context, float fixedDeltaTime);
        void OnExit(TContext context);
    }
}
