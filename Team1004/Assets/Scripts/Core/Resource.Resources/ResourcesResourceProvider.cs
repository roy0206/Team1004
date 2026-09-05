using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ResourcesResourceProvider : IResourceProvider
{
    private sealed class ReferenceState
    {
        public Object Asset;
        public int Count;
    }

    private readonly Dictionary<int, ReferenceState> references = new();

    public async Awaitable<IReadOnlyList<LoadedResource>> LoadLabelAsync(string label)
    {
        var path = NormalizePath(label);
        await Awaitable.MainThreadAsync();

        var assets = Resources.LoadAll<Object>(path);
        var result = new List<LoadedResource>(assets.Length);

        for (var i = 0; i < assets.Length; i++)
        {
            var asset = assets[i];
            if (asset is Component)
                continue;

            AddReference(asset);
            result.Add(new LoadedResource(asset.name, asset));
        }

        return result;
    }

    public void Release(LoadedResource resource)
    {
        var asset = resource.Asset;
        if (asset == null)
            return;

        var id = asset.GetInstanceID();
        if (!references.TryGetValue(id, out var state))
            return;

        state.Count--;
        if (state.Count > 0)
            return;

        references.Remove(id);
        Unload(asset);
    }

    public void ReleaseAll()
    {
        foreach (var state in references.Values)
            Unload(state.Asset);

        references.Clear();
    }

    private void AddReference(Object asset)
    {
        if (asset == null)
            return;

        var id = asset.GetInstanceID();
        if (references.TryGetValue(id, out var state))
        {
            state.Count++;
            return;
        }

        references.Add(id, new ReferenceState
        {
            Asset = asset,
            Count = 1
        });
    }

    private static void Unload(Object asset)
    {
        if (asset == null || asset is GameObject || asset is Component)
            return;

        Resources.UnloadAsset(asset);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Resources directory path is empty.", nameof(path));

        if (path.Contains('\\'))
            throw new ArgumentException("Resources directory path must use forward slashes.", nameof(path));

        return path.Trim().Trim('/');
    }
}
