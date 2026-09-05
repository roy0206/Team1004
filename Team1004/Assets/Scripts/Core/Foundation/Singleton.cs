using UnityEngine;

[DisallowMultipleComponent]
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T instance;
    private static bool applicationIsQuitting;
    private static bool missingInstanceWasReported;

    static Singleton()
    {
        SingletonStatics.Register(ResetStatics);
    }

    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
                return null;

            if (instance == null && !missingInstanceWasReported)
            {
                missingInstanceWasReported = true;
                Debug.LogError(
                    $"[{typeof(T).Name}] No singleton instance is registered. " +
                    "Place one in the startup scene or a bootstrap scene before using it.");
            }

            return instance;
        }
    }

    public static bool HasInstance => !applicationIsQuitting && instance != null;

    public static bool TryGetInstance(out T value)
    {
        value = applicationIsQuitting ? null : instance;
        return value != null;
    }

    private void Awake()
    {
        applicationIsQuitting = false;
        missingInstanceWasReported = false;

        T self = (T)this;
        if (instance != null && instance != self)
        {
            Debug.LogWarning(
                $"[{typeof(T).Name}] A duplicate singleton component was removed.",
                this);

            enabled = false;
            Destroy(this);
            return;
        }

        instance = self;

        if (transform.parent != null)
            transform.SetParent(null, true);

        DontDestroyOnLoad(gameObject);
        OnRegistered();
    }

    private void OnDestroy()
    {
        T self = (T)this;
        if (instance != self)
            return;

        OnUnregistering();
        instance = null;
    }

    private void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }

    protected virtual void OnRegistered()
    {
    }

    protected virtual void OnUnregistering()
    {
    }

    private static void ResetStatics()
    {
        instance = null;
        applicationIsQuitting = false;
        missingInstanceWasReported = false;
    }
}
