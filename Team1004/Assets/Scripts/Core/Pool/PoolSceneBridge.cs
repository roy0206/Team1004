using System;

public sealed class PoolSceneBridge : ISceneTransitionListener, IDisposable
{
    private readonly PoolManager poolManager;
    private readonly SceneController sceneController;
    private bool isAttached;

    public PoolSceneBridge(PoolManager poolManager, SceneController sceneController)
    {
        this.poolManager = poolManager ?? throw new ArgumentNullException(nameof(poolManager));
        this.sceneController = sceneController ?? throw new ArgumentNullException(nameof(sceneController));
    }

    public void Attach()
    {
        if (isAttached)
            return;

        sceneController.RegisterListener(this);
        isAttached = true;
    }

    public void Dispose()
    {
        if (!isAttached)
            return;

        sceneController.UnregisterListener(this);
        isAttached = false;
    }

    public void OnSceneLeaving(SceneTransitionContext context)
    {
        poolManager.ReleaseActive();
    }

    public void OnSceneEntered(SceneTransitionContext context)
    {
    }
}
