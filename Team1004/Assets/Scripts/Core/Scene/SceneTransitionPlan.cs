public readonly struct SceneTransitionPlan
{
    public string TargetScenePath { get; }
    public bool UseLoadingScene { get; }
    public string LoadingScenePath { get; }
    public float MinimumLoadingDuration { get; }
    public float CoverDuration { get; }
    public float RevealDuration { get; }

    public SceneTransitionPlan(
        string targetScenePath,
        bool useLoadingScene,
        string loadingScenePath,
        float minimumLoadingDuration,
        float coverDuration,
        float revealDuration)
    {
        TargetScenePath = targetScenePath;
        UseLoadingScene = useLoadingScene;
        LoadingScenePath = loadingScenePath;
        MinimumLoadingDuration = minimumLoadingDuration;
        CoverDuration = coverDuration;
        RevealDuration = revealDuration;
    }
}
