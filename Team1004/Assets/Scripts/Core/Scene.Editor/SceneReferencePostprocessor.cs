using System;
using UnityEditor;
using UnityEngine;

public sealed class SceneReferencePostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (!ContainsScene(movedAssets))
            return;

        Synchronize("t:SceneSettings");
        Synchronize("t:SceneReference");
    }

    private static void Synchronize(string filter)
    {
        var guids = AssetDatabase.FindAssets(filter);

        for (var i = 0; i < guids.Length; i++)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
                continue;

            var serialized = new SerializedObject(asset);
            if (ScenePathSync.SynchronizeAll(serialized))
                serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static bool ContainsScene(string[] assetPaths)
    {
        for (var i = 0; i < assetPaths.Length; i++)
            if (assetPaths[i].EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }
}
