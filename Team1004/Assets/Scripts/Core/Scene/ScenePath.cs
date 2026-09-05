using System;
using UnityEngine;

[Serializable]
public sealed class ScenePath
{
    [SerializeField] private string path;
    [SerializeField] private string guid;

    public string Path => path;
    public string Name => string.IsNullOrWhiteSpace(path)
        ? string.Empty
        : System.IO.Path.GetFileNameWithoutExtension(path);
    public bool IsValid => !string.IsNullOrWhiteSpace(path);

    public ScenePath()
    {
    }

    public ScenePath(string path)
    {
        this.path = path;
    }

    public override string ToString() => path ?? string.Empty;
}
