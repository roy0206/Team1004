using System.Collections.Generic;
using UnityEngine;

public interface IResourceProvider
{
    Awaitable<IReadOnlyList<LoadedResource>> LoadLabelAsync(string label);
    void Release(LoadedResource resource);
    void ReleaseAll();
}
