using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ResourceManager : Singleton<ResourceManager>
{
    private sealed class CachedResource
    {
        public Object Asset;
        public int ReferenceCount;
    }

    private sealed class LoadedLabel
    {
        public IReadOnlyList<LoadedResource> Resources;
        public IReadOnlyList<string> Keys;
    }

    private readonly Dictionary<string, List<CachedResource>> resources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LoadedLabel> labels = new(StringComparer.Ordinal);
    private readonly HashSet<string> loadingLabels = new(StringComparer.Ordinal);
    private readonly HashSet<string> missingKeyWarnings = new(StringComparer.Ordinal);

    private IResourceProvider provider;
    private int epoch;

    public bool IsInitialized => provider != null;
    public int LoadedResourceCount
    {
        get
        {
            var count = 0;
            foreach (var entries in resources.Values)
                count += entries.Count;

            return count;
        }
    }
    public int LoadedLabelCount => labels.Count;

    protected override void OnUnregistering()
    {
        ReleaseAll();
        provider = null;
    }

    public void Initialize(IResourceProvider resourceProvider)
    {
        if (resourceProvider == null)
            throw new ArgumentNullException(nameof(resourceProvider));

        if (provider == resourceProvider)
            return;

        if (provider != null)
            throw new InvalidOperationException("ResourceManager is already initialized.");

        provider = resourceProvider;
    }

    public async Awaitable LoadLabelAsync(string label)
    {
        EnsureInitialized();
        ValidateLabel(label);

        if (labels.ContainsKey(label))
            return;

        while (loadingLabels.Contains(label))
            await Awaitable.NextFrameAsync();

        if (labels.ContainsKey(label))
            return;

        loadingLabels.Add(label);
        var activeProvider = provider;
        var startEpoch = epoch;
        IReadOnlyList<LoadedResource> loaded = null;

        try
        {
            loaded = await activeProvider.LoadLabelAsync(label);

            if (epoch != startEpoch || provider != activeProvider)
            {
                if (loaded != null)
                    ReleaseProviderResources(activeProvider, loaded);

                return;
            }

            if (loaded == null)
                throw new InvalidOperationException($"Resource provider returned null for label '{label}'.");

            RegisterLabel(label, loaded);
        }
        catch
        {
            if (loaded != null)
                ReleaseProviderResources(activeProvider, loaded);

            throw;
        }
        finally
        {
            if (epoch == startEpoch)
                loadingLabels.Remove(label);
        }
    }

    public T Get<T>(string key) where T : Object
    {
        if (TryGet(key, out T asset))
            return asset;

        if (missingKeyWarnings.Add(key ?? string.Empty))
            Debug.LogWarning($"[ResourceManager] Resource is not loaded: '{key}'.");

        return null;
    }

    public bool TryGet<T>(string key, out T asset) where T : Object
    {
        asset = null;
        if (string.IsNullOrWhiteSpace(key) || !resources.TryGetValue(key, out var entries))
            return false;

        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].Asset.GetType() == typeof(T))
            {
                asset = (T)entries[i].Asset;
                return true;
            }

            if (asset == null && entries[i].Asset is T typedAsset)
                asset = typedAsset;
        }

        return asset != null;
    }

    public IReadOnlyList<string> GetKeys(string label)
    {
        return labels.TryGetValue(label, out var entry)
            ? entry.Keys
            : Array.Empty<string>();
    }

    public IReadOnlyList<T> GetAll<T>(string label) where T : Object
    {
        if (!labels.TryGetValue(label, out var entry))
            return Array.Empty<T>();

        var result = new List<T>();
        for (var i = 0; i < entry.Resources.Count; i++)
            if (entry.Resources[i].Asset is T asset)
                result.Add(asset);

        return result;
    }

    public bool IsLabelLoaded(string label)
    {
        return !string.IsNullOrWhiteSpace(label) && labels.ContainsKey(label);
    }

    public bool IsLabelLoading(string label)
    {
        return !string.IsNullOrWhiteSpace(label) && loadingLabels.Contains(label);
    }

    public void ReleaseLabel(string label)
    {
        if (!labels.Remove(label, out var loadedLabel))
            return;

        for (var i = 0; i < loadedLabel.Resources.Count; i++)
        {
            var resource = loadedLabel.Resources[i];
            RemoveReference(resource);
            provider.Release(resource);
        }
    }

    public void ReleaseAll()
    {
        if (provider == null)
            return;

        foreach (var label in labels.Values)
            ReleaseProviderResources(provider, label.Resources);

        labels.Clear();
        resources.Clear();
        loadingLabels.Clear();
        missingKeyWarnings.Clear();
        epoch++;
        provider.ReleaseAll();
    }

    private void RegisterLabel(string label, IReadOnlyList<LoadedResource> loaded)
    {
        var batchEntries = new HashSet<(string, Type)>();
        var keys = new List<string>(loaded.Count);

        for (var i = 0; i < loaded.Count; i++)
        {
            var resource = loaded[i];
            if (string.IsNullOrWhiteSpace(resource.Key))
                throw new InvalidOperationException($"Label '{label}' contains a resource with an empty key.");

            if (resource.Asset == null)
                throw new InvalidOperationException($"Label '{label}' contains a null resource for '{resource.Key}'.");

            var assetType = resource.Asset.GetType();
            if (!batchEntries.Add((resource.Key, assetType)))
                throw new InvalidOperationException(
                    $"Duplicate resource key in label '{label}': '{resource.Key}' ({assetType.Name}).");

            var cached = FindCached(resource.Key, assetType);
            if (cached != null && cached.Asset != resource.Asset)
                throw new InvalidOperationException(
                    $"Resource key is already assigned to another asset: '{resource.Key}' ({assetType.Name}).");

            if (!keys.Contains(resource.Key))
                keys.Add(resource.Key);
        }

        for (var i = 0; i < loaded.Count; i++)
            AddReference(loaded[i]);

        labels.Add(label, new LoadedLabel
        {
            Resources = loaded,
            Keys = keys.AsReadOnly()
        });
        missingKeyWarnings.Clear();
    }

    private void AddReference(LoadedResource resource)
    {
        if (!resources.TryGetValue(resource.Key, out var entries))
        {
            entries = new List<CachedResource>(1);
            resources.Add(resource.Key, entries);
        }

        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].Asset != resource.Asset)
                continue;

            entries[i].ReferenceCount++;
            return;
        }

        entries.Add(new CachedResource
        {
            Asset = resource.Asset,
            ReferenceCount = 1
        });
    }

    private void RemoveReference(LoadedResource resource)
    {
        if (!resources.TryGetValue(resource.Key, out var entries))
            return;

        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].Asset != resource.Asset)
                continue;

            entries[i].ReferenceCount--;
            if (entries[i].ReferenceCount <= 0)
                entries.RemoveAt(i);

            break;
        }

        if (entries.Count == 0)
            resources.Remove(resource.Key);
    }

    private CachedResource FindCached(string key, Type assetType)
    {
        if (!resources.TryGetValue(key, out var entries))
            return null;

        for (var i = 0; i < entries.Count; i++)
            if (entries[i].Asset.GetType() == assetType)
                return entries[i];

        return null;
    }

    private static void ReleaseProviderResources(IResourceProvider target, IReadOnlyList<LoadedResource> loaded)
    {
        for (var i = 0; i < loaded.Count; i++)
            target.Release(loaded[i]);
    }

    private void EnsureInitialized()
    {
        if (provider == null)
            throw new InvalidOperationException("ResourceManager is not initialized.");
    }

    private static void ValidateLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Resource label is empty.", nameof(label));
    }
}
