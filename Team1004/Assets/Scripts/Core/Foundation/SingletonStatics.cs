using System;
using UnityEngine;

internal static class SingletonStatics
{
    private static Action resetActions;

    public static void Register(Action reset)
    {
        resetActions += reset;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAll()
    {
        resetActions?.Invoke();
    }
}
