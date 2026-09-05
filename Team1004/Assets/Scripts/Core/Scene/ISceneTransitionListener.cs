public interface ISceneTransitionListener
{
    void OnSceneLeaving(SceneTransitionContext context);
    void OnSceneEntered(SceneTransitionContext context);
}
