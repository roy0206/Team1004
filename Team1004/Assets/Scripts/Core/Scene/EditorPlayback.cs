public static class EditorPlayback
{
    public const string ReturnSceneKey = "Core.Scene.EditorPlayback.ReturnScene";

    public static bool TryGetReturnScenePath(out string scenePath)
    {
#if UNITY_EDITOR
        scenePath = UnityEditor.SessionState.GetString(ReturnSceneKey, string.Empty);
        return !string.IsNullOrWhiteSpace(scenePath);
#else
        scenePath = null;
        return false;
#endif
    }
}
