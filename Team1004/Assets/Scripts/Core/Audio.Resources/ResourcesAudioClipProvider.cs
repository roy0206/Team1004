sing System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ResourcesAudioClipProvider : IAudioClipProvider
{
    private readonly HashSet<AudioClip> loadedClips = new();

    public async Awaitable<IReadOnlyDictionary<string, AudioClip>> LoadAsync(
        IReadOnlyList<AudioEntryData> entries)
    {
        Release();

        var result = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        if (entries == null)
            return result;

        var requests = new ResourceRequest[entries.Count];
        for (var i = 0; i < entries.Count; i++)
            requests[i] = Resources.LoadAsync<AudioClip>(entries[i].LoadKey);

        try
        {
            for (var i = 0; i < entries.Count; i++)
            {
                await Awaitable.FromAsyncOperation(requests[i]);

                var clip = requests[i].asset as AudioClip;
                if (clip == null)
                    throw new InvalidOperationException(
                        $"AudioClip was not found in Resources: '{entries[i].LoadKey}'.");

                result[entries[i].Id] = clip;
                loadedClips.Add(clip);
            }

            return result;
        }
        catch
        {
            Release();
            throw;
        }
    }

    public void Release()
    {
        foreach (var clip in loadedClips)
            if (clip != null)
                Resources.UnloadAsset(clip);

        loadedClips.Clear();
    }
}
