using System;
using UnityEngine;
using Object = UnityEngine.Object;

public readonly struct LoadedResource : IEquatable<LoadedResource>
{
    public string Key { get; }
    public Object Asset { get; }

    public LoadedResource(string key, Object asset)
    {
        Key = key;
        Asset = asset;
    }

    public bool Equals(LoadedResource other)
    {
        return string.Equals(Key, other.Key, StringComparison.Ordinal) &&
               Asset == other.Asset;
    }

    public override bool Equals(object obj)
    {
        return obj is LoadedResource other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Asset);
    }
}
