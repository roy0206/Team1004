using System;
using UnityEngine;

[Serializable]
public sealed class SceneTransitionValues
{
    [SerializeField] private bool useLoadingScene;
    [SerializeField] private ScenePath loadingScene = new();
    [SerializeField, Min(0f)] private float minimumLoadingDuration;
    [SerializeField, Min(0f)] private float coverDuration = 0.3f;
    [SerializeField, Min(0f)] private float revealDuration = 0.3f;

    public bool UseLoadingScene => useLoadingScene;
    public ScenePath LoadingScene => loadingScene;
    public float MinimumLoadingDuration => minimumLoadingDuration;
    public float CoverDuration => coverDuration;
    public float RevealDuration => revealDuration;

    public SceneTransitionPlan ToPlan(string targetScenePath)
    {
        var loadingPath = loadingScene?.Path;
        var canUseLoadingScene = useLoadingScene && !string.IsNullOrWhiteSpace(loadingPath);

        return new SceneTransitionPlan(
            targetScenePath,
            canUseLoadingScene,
            loadingPath,
            Mathf.Max(0f, minimumLoadingDuration),
            Mathf.Max(0f, coverDuration),
            Mathf.Max(0f, revealDuration));
    }
}
