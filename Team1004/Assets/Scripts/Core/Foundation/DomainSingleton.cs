using UnityEngine;

[DisallowMultipleComponent]
public abstract class DomainSingleton<T> : MonoBehaviour where T : DomainSingleton<T>
{
    private static T current;
    private static bool missingCurrentWasReported;

    static DomainSingleton()
    {
        SingletonStatics.Register(ResetStatics);
    }

    public static T Current
    {
        get
        {
            if (current == null && !missingCurrentWasReported)
            {
                missingCurrentWasReported = true;
                Debug.LogWarning(
                    $"[{typeof(T).Name}] No domain instance is registered. " +
                    "Use TryGetCurrent or HasCurrent where the domain can be absent.");
            }

            return current;
        }
    }

    public static bool HasCurrent => current != null;

    public static bool TryGetCurrent(out T value)
    {
        value = current;
        return value != null;
    }

    private void Awake()
    {
        T self = (T)this;
        if (current != null && current != self)
        {
            Debug.LogWarning(
                $"[{typeof(T).Name}] A duplicate domain singleton component was removed.",
                this);

            enabled = false;
            Destroy(this);
            return;
        }

        current = self;
        missingCurrentWasReported = false;
        OnRegistered();
    }

    private void OnDestroy()
    {
        T self = (T)this;
        if (current != self)
            return;

        OnUnregistering();
        current = null;
    }

    protected virtual void OnRegistered()
    {
    }

    protected virtual void OnUnregistering()
    {
    }

    private static void ResetStatics()
    {
        current = null;
        missingCurrentWasReported = false;
    }
}
