using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayFromBootstrap
{
    private const string EnabledMenu = "Core/Play From Bootstrap/Enabled";
    private const string UseCurrentMenu = "Core/Play From Bootstrap/Use Current Scene As Bootstrap";

    private static bool duplicateSettingsReported;

    private static string EnabledKey => PlayerSettings.productGUID + ".Core.Scene.PlayFromBootstrap.Enabled";

    static PlayFromBootstrap()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += Apply;
    }

    public static bool Enabled
    {
        get => EditorPrefs.GetBool(EnabledKey, true);
        set
        {
            EditorPrefs.SetBool(EnabledKey, value);
            Apply();
        }
    }

    public static string BootstrapScenePath =>
        TryGetSettings(out var settings) && settings.BootstrapScene.IsValid
            ? settings.BootstrapScene.Path
            : string.Empty;

    public static bool TryGetSettings(out SceneSettings settings)
    {
        var guids = AssetDatabase.FindAssets("t:SceneSettings");
        Array.Sort(guids, CompareByPath);

        settings = null;

        for (var i = 0; i < guids.Length; i++)
        {
            var candidate = AssetDatabase.LoadAssetAtPath<SceneSettings>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (candidate == null)
                continue;

            settings = candidate;
            break;
        }

        if (guids.Length > 1 && !duplicateSettingsReported)
        {
            duplicateSettingsReported = true;
            Debug.LogWarning(
                $"[PlayFromBootstrap] {guids.Length} SceneSettings assets exist. " +
                $"Using '{AssetDatabase.GetAssetPath(settings)}'.",
                settings);
        }

        return settings != null;
    }

    [MenuItem(UseCurrentMenu)]
    private static void UseCurrentScene()
    {
        var path = SceneManager.GetActiveScene().path;
        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.LogWarning("[PlayFromBootstrap] Save the active scene before using it as the Bootstrap scene.");
            return;
        }

        if (!TryGetSettings(out var settings))
        {
            Debug.LogWarning("[PlayFromBootstrap] No SceneSettings asset exists. Create one first.");
            return;
        }

        var serialized = new SerializedObject(settings);
        serialized.FindProperty("bootstrapScene.path").stringValue = path;
        serialized.FindProperty("bootstrapScene.guid").stringValue = AssetDatabase.AssetPathToGUID(path);
        serialized.ApplyModifiedProperties();
        AssetDatabase.SaveAssetIfDirty(settings);

        Apply();
        Debug.Log($"[PlayFromBootstrap] Bootstrap scene set to '{path}' in '{AssetDatabase.GetAssetPath(settings)}'.");
    }

    [MenuItem(EnabledMenu)]
    private static void ToggleEnabled()
    {
        Enabled = !Enabled;
    }

    [MenuItem(EnabledMenu, true)]
    private static bool ValidateToggleEnabled()
    {
        Menu.SetChecked(EnabledMenu, Enabled);
        return true;
    }

    public static bool IsSuppressedByTestRun
    {
        get
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var i = 0; i < arguments.Length; i++)
                if (string.Equals(arguments[i], "-runTests", StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
    }

    private static void Apply()
    {
        if (IsSuppressedByTestRun)
        {
            EditorSceneManager.playModeStartScene = null;
            return;
        }

        var path = BootstrapScenePath;
        var sceneAsset = Enabled && !string.IsNullOrWhiteSpace(path)
            ? AssetDatabase.LoadAssetAtPath<SceneAsset>(path)
            : null;

        EditorSceneManager.playModeStartScene = sceneAsset;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        if (!Enabled || EditorSceneManager.playModeStartScene == null)
        {
            SessionState.EraseString(EditorPlayback.ReturnSceneKey);
            return;
        }

        SessionState.SetString(EditorPlayback.ReturnSceneKey, SceneManager.GetActiveScene().path);
    }

    private static int CompareByPath(string leftGuid, string rightGuid)
    {
        return string.Compare(
            AssetDatabase.GUIDToAssetPath(leftGuid),
            AssetDatabase.GUIDToAssetPath(rightGuid),
            StringComparison.Ordinal);
    }
}
