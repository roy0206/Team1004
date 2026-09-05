public abstract class Module
{
    public MonoThing Host { get; private set; }
    public bool IsAttached => Host != null;
    public bool IsEnabled { get; set; } = true;

    protected abstract ModuleTick Ticks { get; }

    internal ModuleTick TickMask => Ticks;

    internal void Attach(MonoThing host)
    {
        Host = host;
        OnAttached();
    }

    internal void Detach()
    {
        OnDetached();
        Host = null;
    }

    internal void InvokeUpdate() => OnUpdate();
    internal void InvokeLateUpdate() => OnLateUpdate();
    internal void InvokeFixedUpdate() => OnFixedUpdate();
    internal void InvokeInterval() => OnInterval();

    protected virtual void OnAttached()
    {
    }

    protected virtual void OnDetached()
    {
    }

    protected virtual void OnUpdate()
    {
    }

    protected virtual void OnLateUpdate()
    {
    }

    protected virtual void OnFixedUpdate()
    {
    }

    protected virtual void OnInterval()
    {
    }
}
