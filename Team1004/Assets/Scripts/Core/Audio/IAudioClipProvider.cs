using System.Collections.Generic;
using UnityEngine;

public interface IAudioClipProvider
{
    Awaitable<IReadOnlyDictionary<string, AudioClip>> LoadAsync(
        IReadOnlyList<AudioEntryData> entries);

    void Release();
}
