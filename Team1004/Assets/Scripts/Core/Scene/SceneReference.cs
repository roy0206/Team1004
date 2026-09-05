using UnityEngine;

[CreateAssetMenu(fileName = "SceneReference", menuName = "Core/Scene Reference")]
public class SceneReference : ScriptableObject
{
    [SerializeField] private ScenePath scene = new();
    [SerializeField] private bool overrideTransition;
    [SerializeField] private SceneTransitionValues transition = new();

    public ScenePath Scene => scene;
    public string ScenePath => scene?.Path;
    public bool OverrideTransition => overrideTransition;
    public SceneTransitionValues Transition => transition;

    public virtual Awaitable PrepareAsync(SceneTransitionContext context)
    {
        return CompletedAwaitable();
    }

    public virtual Awaitable TeardownAsync(SceneTransitionContext context)
    {
        return CompletedAwaitable();
    }

    private static Awaitable CompletedAwaitable()
    {
        var source = new AwaitableCompletionSource();
        source.SetResult();
        return source.Awaitable;
    }
}
