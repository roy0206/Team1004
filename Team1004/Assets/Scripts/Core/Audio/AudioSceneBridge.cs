using System;

public sealed class AudioSceneBridge : ISceneTransitionListener, IDisposable
{
    private readonly AudioManager audioManager;
    private readonly SceneController sceneController;
    private bool isAttached;

    public AudioSceneBridge(AudioManager audioManager, SceneController sceneController)
    {
        this.audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
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
        audioManager.StopSceneSounds();
    }

    public void OnSceneEntered(SceneTransitionContext context)
    {
    }
}
