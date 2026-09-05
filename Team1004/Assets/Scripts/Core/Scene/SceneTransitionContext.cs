public readonly struct SceneTransitionContext
{
    public string FromScene { get; }
    public string ToScene { get; }

    public SceneTransitionContext(string fromScene, string toScene)
    {
        FromScene = fromScene;
        ToScene = toScene;
    }
}
